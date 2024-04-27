﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Text;

namespace CialloBot;

public class InitPluginSystem(PluginManager pluginManager, ILogger<InitPluginSystem> logger, PluginHelper pluginHelper) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Init Plugins !!!");
        var plugins = await pluginHelper.FindAllInPluginFolderAsync();

        var sb = new StringBuilder();
        sb = sb.Append($"Find {plugins.Count} plugins in {pluginHelper.PluginsFolderPath}");
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

        foreach (var p in plugins)
        {
            logger.LogInformation($"Load plugin {p.Path}");
            pluginManager.LoadPlugin(p.Path);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
