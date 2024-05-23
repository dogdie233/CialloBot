using CommunityToolkit.Diagnostics;

namespace CialloBot.Plugin.ServiceWrapper;

public class SharedService<T>
{
    private readonly T? service;

    public T? Service => service;
    public T RequiredService
    {
        get
        {
            if (service == null)
                ThrowHelper.ThrowInvalidOperationException($"Couldn't find service {typeof(T).FullName}");
            return service;
        }
    }

    public SharedService(SharedServiceContainerProxy proxy) : this((T?)proxy.GetService(typeof(T)))
    {
    }

    internal SharedService(T? service)
    {
        this.service = service;
    }
}
