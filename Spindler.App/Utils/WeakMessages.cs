using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Spindler.Utilities;

public class ThemeChangedMessage(Theme theme) : ValueChangedMessage<Theme>(theme)
{
}

public class ResourceDictionaryUpdatedMessage(ResourceDictionary dictionary) : ValueChangedMessage<ResourceDictionary>(dictionary)
{
}

/// <summary>
/// Message containing a text reference to the key of the color to use for the status bar on android/iOS
/// </summary>
/// <param name="color">The key of the color to use from the theme (if specified), or the default status bar color specified by the theme if null</param>
public class StatusColorUpdateMessage(string? color) : ValueChangedMessage<string?>(color)
{
}
