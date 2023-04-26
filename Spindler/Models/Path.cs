namespace Spindler.Models;

/// <summary>
/// Dataclass representing a selector path, such as an xpath or csspath
/// </summary>
public record Path(string path)
{
    public enum Type
    {
        Css,
        XPath
    }
    public Type type { get; private set; } = path.StartsWith("/") ? Type.XPath : Type.Css;

    public string path { get; private set; } = path;
}
