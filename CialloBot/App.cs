using CialloBot.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CialloBot;

public class App(LagrangeService lagrangeService, ILogger<App> logger, InitPluginSystem ps) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Ciallo~");
        logger.LogInformation("Logging ...");
        await lagrangeService.Login();
        logger.LogInformation("Login succeed, loading plugins ...");
        await ps.Init();
        while (!cancellationToken.IsCancellationRequested) ;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
