using Microsoft.Extensions.DependencyInjection;

namespace CialloBot.Plugin;

public class PluginServiceProviderProxy(PluginServiceContainer container, IKeyedServiceProvider defaultServices, string pluginId) : IPluginServiceContainer
{
    private static readonly Type[] hijackTypes = [typeof(PluginServiceProviderProxy), typeof(IServiceProvider), typeof(IKeyedServiceProvider), typeof(ISupportRequiredService), typeof(IPluginServiceContainer)];

    public object? GetService(Type serviceType)
        => GetKeyedService(serviceType, null);

    public object GetRequiredService(Type serviceType)
        => GetRequiredKeyedService(serviceType, null);

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        if (hijackTypes.Contains(serviceType))
            return this;
        return container.GetKeyedService(serviceType, serviceKey) ?? defaultServices.GetKeyedService(serviceType, serviceKey);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        var service = GetKeyedService(serviceType, serviceKey);
        if (service == null)
            PluginServiceContainer.ThrowServiceNotRegistered(serviceType);

        return service;
    }

    public void RegisterPluginService(Type type, object instance)
        => RegisterKeyedPluginService(type, instance, null);

    public void RegisterKeyedPluginService(Type type, object instance, object? key)
        => container.RegisterKeyed(pluginId, type, instance, key);
}
