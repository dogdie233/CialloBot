using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Reflection;
using System.Runtime.CompilerServices;

namespace CialloBot;

public record struct PluginClassInfo(Type Type, PluginAttribute Attribute);

public class PluginHelper(ILogger<PluginHelper> logger, IHostEnvironment hostEnvironment)
{
    public static readonly string pluginFolder = "plugins";
    public ILogger<PluginHelper> logger = logger;
    public IHostEnvironment hostEnvironment = hostEnvironment;

    public string PluginsFolderPath => Path.Combine(hostEnvironment.ContentRootPath, pluginFolder);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public PluginClassInfo? DetectPlugin(string dllPath)
    {
        var context = new AssemblyCheckerLoadContext(dllPath);
        var assembly = context.LoadFromAssemblyPath(dllPath);
        if (assembly == null)
            throw new Exception($"Couldn't load plugin {dllPath}");

        var pluginInfoUnion = assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(IPlugin)))
            .Select(type => new PluginClassInfo(type, type.GetCustomAttribute<PluginAttribute>()!))
            .Where(info => info.Attribute is not null);

        PluginClassInfo? result = null;
        foreach (var union in pluginInfoUnion)
        {
            if (!result.HasValue)
            {
                result = union!;
                continue;
            }
            return null;  // Have more than 1 plugin class
        }
        pluginInfoUnion = null;
        assembly = null;
        context.Unload();
        return result;
    }

    public Task<List<PluginInfo>> FindAllInPluginFolderAsync()
    {
        logger.LogTrace("Refresh Info List");
        return Task.Run(() =>
        {
            var plugins = new List<PluginInfo>();
            // scan folder
            if (!Directory.Exists(PluginsFolderPath))
                Directory.CreateDirectory(PluginsFolderPath);
            var files = Directory.EnumerateFiles(PluginsFolderPath, "*.dll");

            logger.LogDebug("Scanning plugin info");
            var count = 0;
            foreach (var file in files)
            {
                if (!DetectPlugin(file).TryOut(out var classInfo))
                    continue;

                var info = PluginInfo.CreateFromAttribute(classInfo.Attribute, file);
                var index = plugins.FindIndex(ifo => ifo.Id == info.Id);
                if (index == -1)
                    plugins.Add(info);
                else
                {
                    logger.LogWarning($"Plugin conflicted, dllB will be drop, ID: {info.Id}, dllA: {plugins[index].Path}, dllB: {info.Path}");
                    continue;
                }

                count++;
            }

            logger.LogDebug($"Scan finished, total {count} plugins");
            return plugins;
        });
    }
}
