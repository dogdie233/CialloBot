using CommunityToolkit.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics.CodeAnalysis;

namespace CialloBot;

public class PluginServiceProvider : IServiceProvider, ISupportRequiredService, IKeyedServiceProvider
{
    public record struct ServiceDescription(object service, object? key);

    private Dictionary<Type, List<ServiceDescription>> servicesContainer;
    private ReaderWriterLockSlim rwLock;

    internal PluginServiceProvider()
    {
        servicesContainer = new();
        rwLock = new();
    }

    internal void RegisterService(Type type, object service)
        => RegisterKeyedService(type, service, null);

    internal void RegisterKeyedService(Type type, object service, object? key)
    {
        Guard.IsNotNull(type);
        Guard.IsNotNull(service);

        rwLock.EnterUpgradeableReadLock();
        try
        {
            rwLock.EnterWriteLock();
            if (!servicesContainer.TryGetValue(type, out var list))
            {
                list = new();
                servicesContainer.Add(type, list);
            }
            list.Add(new(service, key));
            rwLock.ExitWriteLock();
        }
        finally
        {
            rwLock.ExitUpgradeableReadLock();
        }
    }

    public void UnregisterService(object obj)
    {
        var waitingRemoveKeys = new List<Type>();
        rwLock.EnterWriteLock();
        try
        {
            foreach (var kvp in servicesContainer)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    if (obj == kvp.Value[i].service)
                        kvp.Value.RemoveAt(i--);
                }
                if (kvp.Value.Count == 0)
                    waitingRemoveKeys.Add(kvp.Key);
            }
            foreach (var key in waitingRemoveKeys)
                servicesContainer.Remove(key);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public object? GetService(Type serviceType)
        => GetKeyedService(serviceType, null);

    public object GetRequiredService(Type serviceType)
        => GetRequiredKeyedService(serviceType, null);

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        Guard.IsNotNull(serviceType);

        rwLock.EnterReadLock();
        try
        {
            if (!servicesContainer.TryGetValue(serviceType, out var serviceList))
                return null;

            for (int i = serviceList.Count - 1; i >= 0; i--)
            {
                if (serviceList[i].key == serviceKey)
                    return serviceList[i].service;
            }
            return null;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        var service = GetKeyedService(serviceType, serviceKey);
        if (service == null)
            ThrowServiceNotRegistered(serviceType);

        return service;
    }

    [DoesNotReturn]
    internal static void ThrowServiceNotRegistered(Type serviceType)
        => ThrowHelper.ThrowInvalidOperationException($"No service for type '{serviceType.Name}' has been registered.");
}
