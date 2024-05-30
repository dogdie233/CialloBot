using CialloBot.Services;
using CialloBot.Utils;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CialloBot;

public class App(LagrangeService lagrangeService, ILogger<App> logger, PluginHelper pluginHelper) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Logging ...");
        await lagrangeService.Login();
        logger.LogInformation("Login succeed, loading plugins ...");
        await pluginHelper.InitPlugins();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        pluginHelper.UnloadAllPlugin();

        lagrangeService.BotContext?.Dispose();
        return Task.CompletedTask;
    }
}
