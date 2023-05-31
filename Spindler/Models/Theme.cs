namespace Spindler.Models;

public enum Themes
{
    Default,
    Dracula,
}
public class Theme
{
    public string safeName;
    public Themes theme;

    public Theme(string safeName, Themes theme)
    {
        this.safeName = safeName;
        this.theme = theme;
    }

    public override string ToString()
    {
        return safeName;
    }

    public static Theme FromThemeType(Themes theme)
    {
        return new Theme(Enum.GetName(typeof(Themes), theme)!, theme);
    }
}
