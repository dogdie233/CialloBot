using Microsoft.Extensions.DependencyInjection;

namespace CialloBot;

public class MixServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        return services;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        var defaultProvider = containerBuilder.BuildServiceProvider();
        var pluginProvider = new PluginServiceProvider();
        return new MixServiceProvider(pluginProvider, defaultProvider);
    }
}
