using CialloBot;
using CialloBot.Models;
using CialloBot.Plugin;
using CialloBot.Services;
using CialloBot.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings()
{
    ApplicationName = "CialloBot",
    ContentRootPath = Path.GetDirectoryName(Environment.ProcessPath),
    Args = args
});

// builder.ConfigureContainer(new MixServiceProviderFactory());

builder.Services.Configure<LagrangeSettingModel>(builder.Configuration.GetSection("Lagrange"));
builder.Services.AddSingleton<PluginServiceContainer>();
builder.Services.AddSingleton<PluginHelper>();
builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<IObjectActivator, ObjectActivator>();
builder.Services.AddSingleton<CialloService>();
builder.Services.AddSingleton<LagrangeService>();
builder.Services.AddSingleton<ILagrangePersistentService, LagrangePersistentService>();

builder.Services.AddHostedService<App>();

var host = builder.Build();

await host.StartAsync();