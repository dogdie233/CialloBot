namespace CialloBot.Utils;

public interface IObjectActivator
{
    public object? TryCreate(Type type, params object?[]? args);
}

public class ObjectActivator : IObjectActivator
{
    public object? TryCreate(Type type, params object?[]? args)
    {
        try
        {
            return Activator.CreateInstance(type, args);
        }
        catch
        {
            return null;
        }
    }
}