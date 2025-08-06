using CialloBot.Plugin;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CialloBot.Utils;
public class PluginHelper(ILogger<PluginHelper> logger, IHostEnvironment hostEnvironment, IServiceProvider serviceProvider)
{
    public static readonly string pluginFolder = "plugins";
    public ILogger<PluginHelper> logger = logger;
    public IHostEnvironment hostEnvironment = hostEnvironment;

    public string PluginsFolderPath => Path.Combine(hostEnvironment.ContentRootPath, pluginFolder);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public PluginAttribute? DetectPlugin(string dllPath)
    {
        var context = new AssemblyCheckerLoadContext(dllPath);
        var assembly = context.LoadFromAssemblyPath(dllPath);
        if (assembly == null)
            throw new Exception($"Couldn't load plugin {dllPath}");

        var plugins = assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(IPlugin)))
            .Select(type => type.GetCustomAttribute<PluginAttribute>())
            .Where(attr => attr is not null);

        PluginAttribute? result = null;
        foreach (var plugin in plugins)
        {
            if (result is null)
            {
                result = plugin;
                continue;
            }
            return null;  // Have more than 1 plugin class
        }

        plugins = null;
        assembly = null;
        context.Unload();

        return result;
    }

    public Type? FindPluginType(Assembly assembly)
    {
        var plugins = assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(IPlugin)))
            .Where(type => type.GetCustomAttribute<PluginAttribute>() != null);

        Type? type = null;
        foreach (var plugin in plugins)
        {
            if (type is null)
            {
                type = plugin;
                continue;
            }
            return null;  // Have more than 1 plugin class
        }

        return type;
    }

    /// <summary>
    /// 异步查找插件文件夹下的所有插件
    /// </summary>
    /// <returns> 一个列表包含所有插件dll的信息 </returns>
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
                var attribute = DetectPlugin(file);
                if (attribute == null)
                    continue;

                var info = PluginInfo.CreateFromAttribute(attribute, file);
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

    public void UnloadAllPlugin()
    {
        var manager = serviceProvider.GetRequiredService<PluginManager>();

        var plugins = manager.LoadedPlugins.ToArray();
        foreach (var plugin in plugins)
            manager.UnloadPlugin(plugin.Info.Id);
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
            try
            {
                pm.TryLoadPlugin(p.Path);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Load plugin {p.Path} failed");
            }
        }
    }

    internal void ConfigPluginServiceCollection(Type pluginType, IServiceCollection collection)
    {
        var method = pluginType.GetMethod(nameof(IPlugin.ConfigService), BindingFlags.Static | BindingFlags.Public)!;
        if (method != null)
            method.Invoke(null, [collection]);
    }
}
