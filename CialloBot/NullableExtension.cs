namespace CialloBot;

public static class NullableExtension
{
    public static bool TryOut<T>(this Nullable<T> nullable, out T output) where T : struct
    {
        output = nullable.HasValue ? nullable.Value : default(T);
        return nullable.HasValue;
    }
}
