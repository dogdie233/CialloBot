using CialloBot;

using Microsoft.Extensions.DependencyInjection;

namespace TestPlugin;

[Plugin("com.github.dogdie233.TestPlugin", "测试插件")]
public class TheTestPlugin(IServiceProvider serviceProvider) : IPlugin
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
