using CialloBot;
using CialloBot.Models;
using CialloBot.Plugin;
using CialloBot.Services;
using CialloBot.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Create host
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings()
{
    ApplicationName = "CialloBot",
    Args = args
});

// Add configuration
builder.Services.Configure<LagrangeSettingModel>(builder.Configuration.GetSection("Lagrange"));

// Add services
builder.Services.AddSingleton<SharedServiceContainer>();
builder.Services.AddSingleton<PluginHelper>();
builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<CialloService>();
builder.Services.AddSingleton<LagrangeService>();
builder.Services.AddSingleton<ILagrangePersistentService, LagrangePersistentService>();

// Main service
builder.Services.AddHostedService<App>();

var host = builder.Build();

// Run
await host.StartAsync();