using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Spindler.Utilities;

public class ThemeChangedMessage : ValueChangedMessage<Theme>
{
    public ThemeChangedMessage(Theme theme) : base(theme) { }
}

public class ResourceDictionaryUpdatedMessage : ValueChangedMessage<ResourceDictionary>
{
    public ResourceDictionaryUpdatedMessage(ResourceDictionary dictionary) : base(dictionary) { }
}

/// <summary>
/// Message containing a text reference to the key of the color to use for the status bar on android/iOS
/// </summary>
public class StatusColorUpdateMessage : ValueChangedMessage<string?>
{
    /// <summary>
    /// Constructor for status color update
    /// </summary>
    /// <param name="color">The key of the color to use (from the current theme), or the default status bar color specified by the theme if null</param>
    public StatusColorUpdateMessage(string? color) : base(color) { }
}
