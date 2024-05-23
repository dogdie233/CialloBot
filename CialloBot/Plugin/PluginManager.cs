using CialloBot.Plugin.ServiceWrapper;
using CialloBot.Services;
using CialloBot.Utils;

using CommunityToolkit.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace CialloBot.Plugin;

public record struct PluginInfo(string Id, string Name, string Path)
{
    public static PluginInfo CreateFromAttribute(PluginAttribute attribute, string path)
        => new PluginInfo(attribute.id, attribute.name, path);
}
public record struct LoadedPlugin(PluginInfo Info, PluginLoadContext Context, ServiceProvider ScopedServices, IPlugin Instance, bool IsDead);

public class PluginManager(PluginHelper pluginHelper, ILogger<PluginManager> logger, SharedServiceContainer sharedServiceContainer, IServiceProvider provider) : IDisposable
{
    private List<LoadedPlugin> loadedPlugins = new();
    private AssemblyLoadContext defaultDependencyContext = AssemblyLoadContext.Default;

    public IReadOnlyList<LoadedPlugin> LoadedPlugins => loadedPlugins.AsReadOnly();

    public void Dispose()
    {
        var plugins = loadedPlugins.Select(plugin => plugin.Info.Id).ToArray();
        foreach (var plugin in plugins)
            UnloadPlugin(plugin);
    }

    public void LoadPlugin(string pluginPath)
    {
        // Detect plugin
        var pluginAttribute = pluginHelper.DetectPlugin(pluginPath);
        if (pluginAttribute is null)
            ThrowHelper.ThrowInvalidOperationException($"Couldn't detect the plugin info in {pluginPath}");

        // Unload old plugin
        var pluginInfo = PluginInfo.CreateFromAttribute(pluginAttribute, pluginPath);
        if (loadedPlugins.Exists(p => p.Info.Id == pluginInfo.Id))
        {
            logger.LogWarning($"plugin {pluginInfo.Id} have been loaded, unloading old");
            UnloadPlugin(pluginInfo.Id);
        }

        // Load plugin assembly
        var context = new PluginLoadContext(ref pluginInfo, defaultDependencyContext, this, pluginHelper);
        var assembly = context.LoadFromAssemblyPath(pluginPath);

        // Create plugin service scoped provider
        var pluginType = pluginHelper.FindPluginType(assembly)!;
        var collection = new ServiceCollection();
        UseDefaultServices(collection, pluginType, pluginAttribute);
        collection.AddSingleton(typeof(IPlugin), pluginType);
        collection.AddSingleton(pluginType);
        pluginHelper.ConfigPluginServiceCollection(pluginType, collection);

        // Create plugin instance
        try
        {
            var pluginServiceProvider = collection.BuildServiceProvider();
            var instance = pluginServiceProvider.GetRequiredService<IPlugin>();

            var loadedPlugin = new LoadedPlugin(pluginInfo, context, pluginServiceProvider, instance, false);

            instance.Startup();
            loadedPlugins.Add(loadedPlugin);
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

        var plugin = loadedPlugins[pluginIndex];
        if (!plugin.IsDead)
            plugin.Instance.Shutdown();
        sharedServiceContainer.Unregister(pluginId);
        plugin.ScopedServices.Dispose();
        plugin.Context.Unload();
        loadedPlugins.RemoveAt(pluginIndex);

        // 其他依赖于这个插件的插件，又如何呢？
    }

    private void UseDefaultServices(IServiceCollection collection, Type pluginType, PluginAttribute attribute)
    {
        var sharedServiceContainerProxy = new SharedServiceContainerProxy(sharedServiceContainer, attribute.id);

        collection.AddLogging();

        collection.AddSingleton<SharedServiceContainerProxy>(sharedServiceContainerProxy);
        collection.AddSingleton<ISharedServiceContainer>(sharedServiceContainerProxy);
        collection.AddTransient(typeof(SharedService<>));
        collection.AddSingleton<LagrangeService>(provider.GetRequiredService<LagrangeService>());

        collection.AddSingleton<PluginAttribute>(attribute);
    }
}