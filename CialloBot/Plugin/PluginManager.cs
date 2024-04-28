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
public record struct LoadedPlugin(PluginInfo Info, PluginLoadContext Context, IPlugin Instance, bool IsDead);

public class PluginManager(PluginHelper pluginHelper, ILogger<PluginManager> logger, IObjectActivator activator, IServiceProvider serviceProvider)
{
    private List<LoadedPlugin> loadedPlugins = new();
    private AssemblyLoadContext defaultDependencyContext = AssemblyLoadContext.Default;
    private PluginServiceContainer pluginServiceContainer = new();

    public IReadOnlyList<LoadedPlugin> LoadedPlugins => loadedPlugins.AsReadOnly();

    public void LoadPlugin(string pluginPath)
    {
        if (!pluginHelper.DetectPlugin(pluginPath).TryOut(out var classInfo))
            ThrowHelper.ThrowInvalidOperationException($"Couldn't detect the plugin info in {pluginPath}");

        var pluginInfo = PluginInfo.CreateFromAttribute(classInfo.Attribute, pluginPath);
        if (loadedPlugins.Exists(p => p.Info.Id == pluginInfo.Id))
        {
            logger.LogWarning($"plugin {pluginInfo.Id} have been loaded, unloading old");
            UnloadPlugin(pluginInfo.Id);
        }

        var context = new PluginLoadContext(ref pluginInfo, defaultDependencyContext, this, pluginHelper);
        var assembly = context.LoadFromAssemblyPath(pluginPath);
        var proxy = new PluginServiceProviderProxy(pluginServiceContainer, (IKeyedServiceProvider)serviceProvider, pluginInfo.Id);
        if (activator.TryCreate(classInfo.Type, [proxy]) is not IPlugin instance)
            throw new Exception($"Couldn't create plugin instance '{classInfo.Type.Name}' for plugin {pluginPath}");

        var loadedPlugin = new LoadedPlugin(pluginInfo, context, instance, false);
        try
        {
            loadedPlugin.Instance.Startup();
            proxy.RegisterPluginService(classInfo.Type, loadedPlugin.Instance);
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
            plugin.Instance?.Shutdown();
        pluginServiceContainer.Unregister(pluginId);
        plugin.Context?.Unload();
        loadedPlugins.RemoveAt(pluginIndex);

        // 其他依赖于这个插件的插件，又如何呢？
    }
}