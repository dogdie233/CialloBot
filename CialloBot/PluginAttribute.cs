namespace CialloBot;

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute(string id, string name) : Attribute
{
    public readonly string id = id;
    public readonly string name = name;
}