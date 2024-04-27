using System.Reflection;
using System.Runtime.Loader;

namespace CialloBot;

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
        this.dependencyResolver = new(info.Path);
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
            throw new Exception($"Couldn't resolve the dependency assembly {assemblyName}");

        var pluginInfo = pluginHelper.DetectPlugin(path);
        if (pluginInfo != null)  // It's a plugin
            pluginManager.LoadPlugin(path);
        else  // It's a normal dependency, just put into the default context
            defaultDependencyContext.LoadFromAssemblyPath(path);

        return null;
    }
}
