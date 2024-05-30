using Microsoft.Extensions.DependencyInjection;

namespace CialloBot.Plugin.ServiceWrapper;

public class SharedServiceContainerProxy : ISharedServiceContainer, IDisposable
{
    private readonly SharedServiceContainer sharedContainer;
    private readonly string pluginId;
    private readonly IServiceScope mainScoped;
    private bool disposedValue = false;

    public SharedServiceContainerProxy(IServiceProvider mainServices, SharedServiceContainer container, string pluginId)
    {
        this.mainScoped = mainServices.CreateScope();
        this.sharedContainer = container;
        this.pluginId = pluginId;
    }

    internal object? GetService(Type type)
    {
        if (disposedValue)
            throw new ObjectDisposedException(ToString());
        return mainScoped.ServiceProvider.GetService(type) ?? sharedContainer.GetService(type);
    }

    public void RegisterPluginService(Type type, object instance)
        => sharedContainer.RegisterKeyedService(pluginId, type, instance, null);

    public void RegisterKeyedPluginService(Type type, object instance, object? key)
        => sharedContainer.RegisterKeyedService(pluginId, type, instance, key);

    public SharedService<T> GetServiceProxy<T>(Type type)
        => new((T?)mainScoped.ServiceProvider.GetService(type) ?? (T?)sharedContainer.GetService(type));

    public SharedService<T> GetKeyedServiceWrapper<T>(Type type, object? key)
        => new((T?)mainScoped.ServiceProvider.GetKeyedServices(type, key) ?? (T?)sharedContainer.GetKeyedService(type, key));

    public void Dispose()
    {
        if (disposedValue)
            return;

        mainScoped.Dispose();
    }
}
