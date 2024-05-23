using CialloBot.Plugin;
using CialloBot.Services;

using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestPlugin;

[Plugin("com.github.dogdie233.TestPlugin", "测试插件")]
public class TheTestPlugin : IPlugin
{
    private readonly ILogger<TheTestPlugin> logger;
    private readonly LagrangeService lagrangeService;

    public TheTestPlugin(ILogger<TheTestPlugin> logger, LagrangeService lagrangeService)
    {
        this.logger = logger;
        this.lagrangeService = lagrangeService;
    }

    public void Startup()
    {
        lagrangeService.BotContext.Invoker.OnGroupMessageReceived += OnGroupMessageReceived;
    }

    public void Shutdown()
    {
        lagrangeService.BotContext.Invoker.OnGroupMessageReceived -= OnGroupMessageReceived;
    }

    private void OnGroupMessageReceived(BotContext bot, GroupMessageEvent @event)
    {
        if (@event.Chain.FirstOrDefault() is not TextEntity text || text is not { Text: "ping" })
            return;

        bot.SendMessage(MessageBuilder.Group(@event.Chain.GroupUin!.Value)
            .Forward(@event.Chain)
            .Text("pong")
            .Build());
    }
}
