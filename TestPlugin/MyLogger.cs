using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using CialloBot;

using Lagrange.Core;
using Lagrange.Core.Message;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace TestPlugin;

public class BotLoggerProvider(IServiceProvider sp) : ILoggerProvider
{
    private ConsoleLoggerProvider? clp;

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_formatters")]
    private static extern ref ConcurrentDictionary<string, ConsoleFormatter> GetFormatters(ConsoleLoggerProvider instance);

    ConcurrentDictionary<string, MyLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        clp ??= (ConsoleLoggerProvider)sp.GetServices<ILoggerProvider>().First(p => p is ConsoleLoggerProvider);
        return _loggers.GetOrAdd(categoryName, new MyLogger(categoryName, GetFormatters(clp)["simple"], null, new ConsoleLoggerOptions()));
    }

    public void Dispose()
    {
    }
}

internal sealed partial class MyLogger : ILogger
{
    public static BotContext? bot;
    private readonly string _name;
    [ThreadStatic] private static StringWriter? t_stringWriter;
    private readonly ConsoleFormatter formatter;

    internal IExternalScopeProvider? ScopeProvider { get; set; }

    internal ConsoleLoggerOptions Options { get; set; }

    internal MyLogger(string name, ConsoleFormatter formatter, IExternalScopeProvider? scopeProvider, ConsoleLoggerOptions options)
    {
        _name = name;
        this.formatter = formatter;
        ScopeProvider = scopeProvider;
        Options = options;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || bot == null)
            return;

        t_stringWriter ??= new StringWriter();

        var logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
        this.formatter.Write(in logEntry, ScopeProvider, t_stringWriter);
        var stringBuilder = t_stringWriter.GetStringBuilder();
        if (stringBuilder.Length == 0)
            return;

        var message = stringBuilder.ToString();
        stringBuilder.Clear();
        if (stringBuilder.Capacity > 1024)
            stringBuilder.Capacity = 1024;

        if (exception is not BotEventException botEventException)
            return;

        var messageBuilder = BakaLogDestFinder.TryMakeDest(botEventException.Event);
        if (messageBuilder == null)
            return;

        message = ShowerString(message);
        messageBuilder.Text(message)
            .GreyTip("你已被移出群聊");
        bot.SendMessage(messageBuilder.Build());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Error;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return ScopeProvider?.Push(state) ?? NullDisposable.instance;
    }

    /// <summary>
    /// Remove color code from string
    /// </summary>
    private static string ShowerString(string str)
        => AnsiColorRegex().Replace(str, "");

    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]")]
    private static partial Regex AnsiColorRegex();
}

internal sealed class NullDisposable : IDisposable
{
    public static readonly NullDisposable instance = new NullDisposable();

    public void Dispose()
    {
    }
}