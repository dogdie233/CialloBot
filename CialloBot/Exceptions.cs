using System.Diagnostics.CodeAnalysis;

using Lagrange.Core.Event;

namespace CialloBot;

public class StartupException : Exception
{
    public StartupException(string pluginId, Exception innerException) : base($"Couldn't startup the plugin {pluginId}", innerException) { }
}

public class BotEventException : Exception
{
    public EventBase Event { get; init; }

    public BotEventException(string eventName, EventBase @event, Exception innerException)
        : base($"An exception was occurred when executing event {eventName}", innerException)
    {
        Event = @event;
    }
}