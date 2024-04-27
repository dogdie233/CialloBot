namespace CialloBot;

public class Exceptions
{
    public class StartupException : Exception
    {
        public StartupException(string pluginId, Exception innerException) : base($"Couldn't startup the plugin {pluginId}", innerException) { }
    }
}
