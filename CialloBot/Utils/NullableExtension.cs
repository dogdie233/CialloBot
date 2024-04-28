namespace CialloBot.Utils;

internal static class NullableExtension
{
    internal static bool TryOut<T>(this T? nullable, out T output) where T : struct
    {
        output = nullable.HasValue ? nullable.Value : default;
        return nullable.HasValue;
    }
}
