using CommunityToolkit.Diagnostics;

using System.Diagnostics.CodeAnalysis;

namespace CialloBot.Plugin;

public class SharedServiceContainer
{
    public record struct ServiceDescription(string RegistrarId, object Service, object? Key);

    private readonly Dictionary<Type, List<ServiceDescription>> servicesContainer;
    private readonly ReaderWriterLockSlim rwLock;

    public SharedServiceContainer()
    {
        servicesContainer = new();
        rwLock = new();
    }

    internal void RegisterService(string registrarId, Type type, object service)
        => RegisterKeyedService(registrarId, type, service, null);

    internal void RegisterKeyedService(string registrarId, Type type, object service, object? key)
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
            list.Add(new(registrarId, service, key));
            rwLock.ExitWriteLock();
        }
        finally
        {
            rwLock.ExitUpgradeableReadLock();
        }
    }

    internal void Unregister(string registrarId)
    {
        var waitingRemoveKeys = new List<Type>();
        rwLock.EnterWriteLock();
        try
        {
            foreach (var kvp in servicesContainer)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    if (registrarId == kvp.Value[i].RegistrarId)
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

    internal object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        Guard.IsNotNull(serviceType);

        rwLock.EnterReadLock();
        try
        {
            if (!servicesContainer.TryGetValue(serviceType, out var serviceList))
                return null;

            for (int i = serviceList.Count - 1; i >= 0; i--)
            {
                if (serviceList[i].Key == serviceKey)
                    return serviceList[i].Service;
            }
            return null;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    internal object? GetService(Type serviceType)
        => GetKeyedService(serviceType, null);

    [DoesNotReturn]
    internal static void ThrowServiceNotRegistered(Type serviceType)
        => ThrowHelper.ThrowInvalidOperationException($"No service for type '{serviceType.Name}' has been registered.");
}