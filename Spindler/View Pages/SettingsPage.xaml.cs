using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Spindler.Behaviors;
namespace Spindler;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        FontSizeEntry.Behaviors.Add(new TextValidationBehavior((string text) =>
        {
            return text.All(c => c >= '0' && c <= '9');
        }));
    }
}