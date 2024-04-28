using Microsoft.Extensions.DependencyInjection;

namespace CialloBot.Plugin
{
    public interface IPluginServiceContainer : IServiceProvider, ISupportRequiredService, IKeyedServiceProvider
    {
        public void RegisterPluginService(Type type, object instance);
        public void RegisterKeyedPluginService(Type type, object instance, object? key);
    }
}
