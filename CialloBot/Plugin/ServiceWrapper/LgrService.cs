using CialloBot.Services;

using Lagrange.Core;
using Lagrange.Core.Event;

using Microsoft.Extensions.Logging;

namespace CialloBot.Plugin.ServiceWrapper;

public partial class LgrService : IDisposable
{
    private readonly string pluginId;
    private readonly SharedService<LagrangeService> lagrange;
    private readonly ILogger<LgrService> logger;

    public BotContext BotContext => lagrange.RequiredService.BotContext;
    private EventInvoker GlobalInvoker => BotContext.Invoker;

    public LgrService(SharedService<LagrangeService> lagrange, PluginAttribute attribute, ILogger<LgrService> logger)
    {
        this.pluginId = attribute.id;
        this.lagrange = lagrange;
        this.logger = logger;

        RegisterEvents();
    }

    partial void RegisterEvents();
    partial void UnregisterEvents();

    public void Dispose()
    {
        UnregisterEvents();
    }
}