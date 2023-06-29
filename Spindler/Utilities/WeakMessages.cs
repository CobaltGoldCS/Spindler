using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Spindler.Models;

namespace Spindler.Utilities;

public class ThemeChangedMessage : ValueChangedMessage<Theme>
{
    public ThemeChangedMessage(Theme theme) : base(theme) { }
}

public class ResourceDictionaryUpdatedMessage : ValueChangedMessage<ResourceDictionary>
{
    public ResourceDictionaryUpdatedMessage(ResourceDictionary dictionary) : base(dictionary) { }
}
