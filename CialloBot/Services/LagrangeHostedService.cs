using Microsoft.Extensions.Hosting;

namespace CialloBot.Services;

public class LagrangeHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Lagr");
        return Task.CompletedTask;
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
