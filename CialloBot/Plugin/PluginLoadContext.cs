using CialloBot.Utils;

using System.Reflection;
using System.Runtime.Loader;

namespace CialloBot.Plugin;

public class PluginLoadContext : AssemblyLoadContext
{
    private AssemblyLoadContext defaultDependencyContext;
    private PluginManager pluginManager;
    private PluginHelper pluginHelper;
    private AssemblyDependencyResolver dependencyResolver;

    public PluginLoadContext(ref readonly PluginInfo info,
        AssemblyLoadContext defaultDependencyContext,
        PluginManager pluginManager,
        PluginHelper pluginHelper) : base(info.Id, true)
    {
        this.defaultDependencyContext = defaultDependencyContext;
        this.pluginManager = pluginManager;
        this.pluginHelper = pluginHelper;
        dependencyResolver = new(info.Path);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        foreach (var loadedDependencyAssembly in defaultDependencyContext.Assemblies)
        {
            if (AssemblyName.ReferenceMatchesDefinition(assemblyName, loadedDependencyAssembly.GetName()))
                return loadedDependencyAssembly;
        }

        foreach (var loadedPlugin in pluginManager.LoadedPlugins)
        {
            var pluginAssembly = loadedPlugin.Instance.GetType().Assembly;
            if (AssemblyName.ReferenceMatchesDefinition(assemblyName, pluginAssembly.GetName()))
                return pluginAssembly;
        }

        // Dependency have not be loaded
        var path = dependencyResolver.ResolveAssemblyToPath(assemblyName);
        if (path == null)  // Couldn't find dll path
            throw new FileNotFoundException($"Couldn't resolve the dependency assembly", assemblyName.ToString());

        var pluginInfo = pluginHelper.DetectPlugin(path);
        if (pluginInfo != null)  // It's a plugin
            pluginManager.LoadPlugin(path);

        return null;
    }
}
