namespace Spindler.Utilities;

public static class Extensions
{
    public static Return GetOrDefault<Key, Return>(this Dictionary<Key, object> dictionary, Key key, Return @default) where Key : notnull
    {
        if (dictionary is null) return @default;
        return dictionary.TryGetValue(key, out object? value) ? (Return)value : @default;
    }
}
