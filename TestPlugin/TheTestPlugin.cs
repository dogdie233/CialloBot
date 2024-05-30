using CialloBot.Plugin;
using CialloBot.Plugin.ServiceWrapper;

using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace TestPlugin;

[Plugin("com.github.dogdie233.TestPlugin", "测试插件")]
public class TheTestPlugin : IPlugin
{
    private readonly ILogger<TheTestPlugin> logger;
    private readonly LgrService lgr;

    public TheTestPlugin(ILogger<TheTestPlugin> logger, LgrService lgr)
    {
        this.logger = logger;
        this.lgr = lgr;
    }

    public void Startup()
    {
        lgr.OnGroupMessageReceived += OnGroupMessageReceived;
        MyLogger.bot = lgr.BotContext;
    }

    public void Shutdown()
    {
        lgr.OnGroupMessageReceived -= OnGroupMessageReceived;
    }

    private void OnGroupMessageReceived(BotContext bot, GroupMessageEvent @event)
    {
        if (@event.Chain.FirstOrDefault() is TextEntity text)
        {
            if (text.Text == "ping")
            {
                bot.SendMessage(MessageBuilder.Group(@event.Chain.GroupUin!.Value)
                    .Forward(@event.Chain)
                    .Text("pong")
                    .Build());
            }
            else if (text.Text == "throw")
            {
                throw new Exception("Test exception");
            }
        }        
    }

    public static void ConfigService(IServiceCollection collection)
    {
        collection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BotLoggerProvider>());
        });
    }
}