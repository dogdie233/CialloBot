using CialloBot;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings()
{
    ApplicationName = "CialloBot",
    ContentRootPath = Path.GetDirectoryName(Environment.ProcessPath),
    Args = args
});

builder.Services.AddSingleton<IServiceProviderFactory<IServiceCollection>, MixServiceProviderFactory>();
builder.Services.AddSingleton<PluginHelper>();
builder.Services.AddSingleton<PluginManager>();
builder.Services.AddSingleton<IObjectActivator, ObjectActivator>();
builder.Services.AddSingleton<CialloService>();

builder.Services.AddHostedService<InitPluginSystem>();
builder.Services.AddHostedService<LagrangeHostedService>();

var host = builder.Build();

await host.StartAsync();