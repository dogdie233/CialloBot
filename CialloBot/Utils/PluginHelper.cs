using CialloBot.Plugin;

using Lagrange.Core.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CialloBot.Utils;

public record struct PluginClassInfo(Type Type, PluginAttribute Attribute);

public class PluginHelper(ILogger<PluginHelper> logger, IHostEnvironment hostEnvironment, IServiceProvider serviceProvider)
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

    internal async Task InitPlugins()
    {
        var plugins = await FindAllInPluginFolderAsync();

        var sb = new StringBuilder();
        sb = sb.Append($"Find {plugins.Count} plugins in {PluginsFolderPath}");
        AddLineFormat("ID", "Name");
        foreach (var p in plugins)
            AddLineFormat(p.Id, p.Name);

        void AddLineFormat(string id, string name)
        {
            sb.AppendLine();
            sb.Append(id.PadRight(48, ' '));
            sb.Append(' ', 4);
            sb.Append(name);
        }
        logger.LogInformation(sb.ToString());

        var pm = serviceProvider.GetRequiredService<PluginManager>();
        foreach (var p in plugins)
        {
            logger.LogInformation($"Load plugin {p.Path}");
            pm.LoadPlugin(p.Path);
        }
    }
}
