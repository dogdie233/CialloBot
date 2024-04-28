using CialloBot.Plugin;
using CialloBot.Services;

using Microsoft.Extensions.DependencyInjection;

namespace TestPlugin;

[Plugin("com.github.dogdie233.TestPlugin", "测试插件")]
public class TheTestPlugin(IPluginServiceContainer serviceProvider) : IPlugin
{
    public void Startup()
    {
        serviceProvider.GetRequiredService<CialloService>().Print(nameof(TheTestPlugin));
    }

    public void Shutdown()
    {
        serviceProvider.GetRequiredService<CialloService>().Print(nameof(TheTestPlugin) + " Bye~");
    }
}
