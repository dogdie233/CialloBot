namespace CialloBot.Plugin.ServiceWrapper;

public class SharedServiceContainerProxy(SharedServiceContainer container, string pluginId) : ISharedServiceContainer
{
    internal object? GetService(Type type)
        => container.GetService(type);

    public void RegisterPluginService(Type type, object instance)
        => container.RegisterKeyedService(pluginId, type, instance, null);

    public void RegisterKeyedPluginService(Type type, object instance, object? key)
        => container.RegisterKeyedService(pluginId, type, instance, key);

    public SharedService<T> GetServiceProxy<T>(Type type)
        => new((T?)container.GetService(type));

    public SharedService<T> GetKeyedServiceWrapper<T>(Type type, object? key)
        => new((T?)container.GetKeyedService(type, key));
}
