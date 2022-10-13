namespace Spindler.Utils;

public static class Extensions
{
    public static Return GetOrDefault<Key, Return>(this Dictionary<Key, object> dict, Key key, Return @default) where Key : notnull
    {
        if (dict is null) return @default;
        return dict.TryGetValue(key, out object? value) ? (Return)value : @default;
    }
}
