namespace Spindler.Utils
{
    public static class Extensions
    {
        public static Value GetOrDefault<Key, Value>(this Dictionary<Key, Value> dict, Key key, Value @default)
        {
            Value value;
            return dict.TryGetValue(key, out value) ? value : @default;
        }
    }
}
