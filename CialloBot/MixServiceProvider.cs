using Microsoft.Extensions.DependencyInjection;

namespace CialloBot;

public sealed class MixServiceProvider : IServiceProvider, ISupportRequiredService, IKeyedServiceProvider
{
    private readonly PluginServiceProvider pluginServices;
    private readonly ServiceProvider defaultProvider;

    public MixServiceProvider(PluginServiceProvider pluginServices, ServiceProvider defaultProvider)
    {
        this.pluginServices = pluginServices;
        this.defaultProvider = defaultProvider;
    }

    public object? GetService(Type serviceType)
        => GetKeyedService(serviceType, null);

    public object GetRequiredService(Type serviceType)
        => GetRequiredKeyedService(serviceType, null);

    public object? GetKeyedService(Type serviceType, object? serviceKey)
        => pluginServices.GetKeyedService(serviceType, serviceKey) ?? defaultProvider.GetKeyedService(serviceType, serviceKey);

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        var service = GetKeyedService(serviceType, serviceKey);
        if (service == null)
            PluginServiceProvider.ThrowServiceNotRegistered(serviceType);

        return service;
    }
}
