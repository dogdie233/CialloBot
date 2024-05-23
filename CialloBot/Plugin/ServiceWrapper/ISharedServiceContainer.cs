namespace CialloBot.Plugin.ServiceWrapper
{
    public interface ISharedServiceContainer
    {
        public SharedService<T> GetServiceProxy<T>(Type type);
        public SharedService<T> GetKeyedServiceWrapper<T>(Type type, object? key);
        public void RegisterPluginService(Type type, object instance);
        public void RegisterKeyedPluginService(Type type, object instance, object? key);
    }
}
