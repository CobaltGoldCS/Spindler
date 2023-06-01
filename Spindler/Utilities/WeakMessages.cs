using CommunityToolkit.Mvvm.Messaging.Messages;
using Spindler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spindler.App;

namespace Spindler.Utilities;

public class ThemeChangedMessage : ValueChangedMessage<Theme>
{
    public ThemeChangedMessage(Theme theme) : base(theme) { }
}

public class ResourceDictionaryUpdatedMessage : ValueChangedMessage<ResourceDictionary>
{
    public ResourceDictionaryUpdatedMessage(ResourceDictionary dictionary) : base(dictionary) { }
}