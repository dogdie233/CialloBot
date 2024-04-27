namespace CialloBot;

public interface IObjectActivator
{
    public object? TryCreate(Type type);
}

public class ObjectActivator : IObjectActivator
{
    private IServiceProvider serviceProvider;

    public ObjectActivator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public object? TryCreate(Type type)
    {
        try
        {
            return Activator.CreateInstance(type, [serviceProvider]);
        }
        catch
        {
            return null;
        }
    }
}