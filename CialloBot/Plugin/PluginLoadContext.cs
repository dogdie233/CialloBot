using CialloBot.Utils;

using System.Reflection;
using System.Runtime.Loader;

using Microsoft.Extensions.Logging;

namespace CialloBot.Plugin;

public class PluginLoadContext : AssemblyLoadContext
{
    private AssemblyLoadContext defaultDependencyContext;
    private PluginManager pluginManager;
    private PluginHelper pluginHelper;
    private AssemblyDependencyResolver dependencyResolver;
    private ILogger<PluginLoadContext> logger;

    public PluginLoadContext(ref readonly PluginInfo info,
        AssemblyLoadContext defaultDependencyContext,
        PluginManager pluginManager,
        PluginHelper pluginHelper,
        ILogger<PluginLoadContext> logger) : base(info.Id, true)
    {
        this.defaultDependencyContext = defaultDependencyContext;
        this.pluginManager = pluginManager;
        this.pluginHelper = pluginHelper;
        dependencyResolver = new AssemblyDependencyResolver(info.Path);
        this.logger = logger;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        foreach (var loadedDependencyAssembly in defaultDependencyContext.Assemblies)
        {
            if (AssemblyName.ReferenceMatchesDefinition(assemblyName, loadedDependencyAssembly.GetName()))
                return loadedDependencyAssembly;
        }

        var assembly = ResolveInLoadedPlugins(assemblyName);
        if (assembly is not null)
            return assembly;

        // Dependency have not be loaded
        var path = dependencyResolver.ResolveAssemblyToPath(assemblyName);
        if (path == null) // Couldn't find dll path
        {
            // Try use the default dependency context to load the assembly in system
            try
            {
                return defaultDependencyContext.LoadFromAssemblyName(assemblyName);
            }
            catch (Exception)
            {
                // ignored
            }
            throw new FileNotFoundException($"Couldn't resolve the dependency assembly", assemblyName.ToString());
        }
        
        logger.LogDebug("Resolving dependency assembly {AssemblyName} from path {Path}", assemblyName, path);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Dependency assembly {assemblyName.Name} not found at {path}", path);
        
        if (pluginHelper.DetectPlugin(path) is not null)
        {
            pluginManager.TryLoadPlugin(path);
            assembly = ResolveInLoadedPlugins(assemblyName);
            if (assembly is not null)
                return assembly;
            throw new FileNotFoundException($"Couldn't load the dependency assembly {assemblyName.Name} from plugin {path}");
        }
        return LoadFromAssemblyPath(path);
    }

    private Assembly? ResolveInLoadedPlugins(AssemblyName assemblyName)
    {
        foreach (var loadedPlugin in pluginManager.LoadedPlugins)
        {
            var pluginAssembly = loadedPlugin.Instance.GetType().Assembly;
            if (AssemblyName.ReferenceMatchesDefinition(assemblyName, pluginAssembly.GetName()))
                return pluginAssembly;
        }

        return null;
    }
}
