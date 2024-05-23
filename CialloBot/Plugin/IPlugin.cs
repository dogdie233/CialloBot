using Microsoft.Extensions.DependencyInjection;

namespace CialloBot.Plugin
{
    public interface IPlugin
    {
        public void Startup();
        public void Shutdown();

        static virtual void ConfigService(IServiceCollection collection)
        {
        }
    }
}
