using CialloBot.Plugin.ServiceWrapper;
using CialloBot.Utils;

using CommunityToolkit.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CialloBot.Plugin;

public readonly record struct PluginInfo(string Id, string Name, string Path)
{
    public static PluginInfo CreateFromAttribute(PluginAttribute attribute, string path)
        => new PluginInfo(attribute.id, attribute.name, path);
}
public readonly record struct LoadedPlugin(PluginInfo Info, PluginLoadContext Context, ServiceProvider ScopedServices, IPlugin Instance);

public class PluginManager(PluginHelper pluginHelper,
    ILogger<PluginManager> logger,
    ILogger<PluginLoadContext> loadContextLogger,
    SharedServiceContainer sharedServiceContainer,
    IServiceProvider provider,
    IHostEnvironment env)
{
    private List<LoadedPlugin> loadedPlugins = new();
    private AssemblyLoadContext defaultDependencyContext = AssemblyLoadContext.Default;

    public IReadOnlyList<LoadedPlugin> LoadedPlugins => loadedPlugins.AsReadOnly();

    public void TryLoadPlugin(string pluginPath)
    {
        // Detect plugin
        var pluginAttribute = pluginHelper.DetectPlugin(pluginPath);
        if (pluginAttribute is null)
            return;

        logger.LogInformation($"Loading plugin {pluginPath}");
        // Unload old plugin
        var pluginInfo = PluginInfo.CreateFromAttribute(pluginAttribute, pluginPath);
        if (loadedPlugins.Exists(p => p.Info.Id == pluginInfo.Id))
        {
            logger.LogWarning($"plugin {pluginInfo.Id} have been loaded, unloading old");
            UnloadPlugin(pluginInfo.Id);
        }

        // Load plugin assembly
        var context = new PluginLoadContext(ref pluginInfo, defaultDependencyContext, this, pluginHelper, loadContextLogger);
        Assembly assembly;
        using (var fs = File.OpenRead(pluginPath))
        {
            assembly = context.LoadFromStream(fs);
        }

        // Create plugin service scoped provider
        var pluginType = pluginHelper.FindPluginType(assembly)!;
        var collection = new ServiceCollection();
        UseDefaultServices(collection, pluginType, pluginAttribute);
        pluginHelper.ConfigPluginServiceCollection(pluginType, collection);
        collection.AddSingleton(typeof(IPlugin), pluginType);
        collection.AddSingleton(pluginType);

        // Create plugin instance
        try
        {
            var pluginServiceProvider = collection.BuildServiceProvider();
            var instance = pluginServiceProvider.GetRequiredService<IPlugin>();

            var loadedPlugin = new LoadedPlugin(pluginInfo, context, pluginServiceProvider, instance);

            instance.Startup();
            loadedPlugins.Add(loadedPlugin);

            logger.LogInformation($"Plugin {loadedPlugin.Info.Id} have been loaded");
        }
        catch (Exception ex)
        {
            throw new StartupException(pluginInfo.Id, ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UnloadPlugin(string pluginId)
    {
        var pluginIndex = loadedPlugins.FindIndex(plugin => plugin.Info.Id == pluginId);
        if (pluginIndex == -1)
            return;
        
        logger.LogInformation($"Unloading plugin {pluginId}");

        var plugin = loadedPlugins[pluginIndex];
        plugin.Instance.Shutdown();
        sharedServiceContainer.Unregister(pluginId);
        plugin.ScopedServices.Dispose();
        plugin.Context.Unload();
        plugin = default;
        loadedPlugins.RemoveAt(pluginIndex);

        GC.Collect();

        logger.LogInformation($"Plugin {pluginId} have been unloaded");
        // 其他依赖于这个插件的插件，又如何呢？
    }

    private void UseDefaultServices(IServiceCollection collection, Type pluginType, PluginAttribute attribute)
    {
        var sharedServiceContainerProxy = new SharedServiceContainerProxy(provider, sharedServiceContainer, attribute.id);

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        configBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
        configBuilder.AddJsonFile($"configs/{attribute.id}.json", optional: true, reloadOnChange: true);
        configBuilder.AddEnvironmentVariables();
        var config = configBuilder.Build();

        collection.AddSingleton<IConfiguration>(config);
        collection.AddOptions<PluginAttribute>("QWQ");
        collection.AddLogging(builder =>
        {
            builder.AddConfiguration(config.GetSection("Logging"));
            builder.AddConsole();
        });

        collection.AddSingleton<SharedServiceContainerProxy>(sharedServiceContainerProxy);
        collection.AddSingleton<ISharedServiceContainer>(sharedServiceContainerProxy);
        collection.AddTransient(typeof(SharedService<>));
        collection.AddSingleton<LgrService>();

        collection.AddSingleton<PluginAttribute>(attribute);
    }
}