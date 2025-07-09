using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Spindler.Models;
using Spindler.Services;
using System.Collections.ObjectModel;

namespace Spindler.Views;

public class BookmarkClickedMessage : ValueChangedMessage<Bookmark>
{
    public BookmarkClickedMessage(Bookmark value) : base(value) { }
}

public partial class BookmarkDialog : Popup<Bookmark>
{
    public BookmarkDialog(BookmarkDialogViewmodel viewmodel)
    {
        InitializeComponent();
        BindingContext = viewmodel;
        WidthRequest = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        HeightRequest = .5 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density);
    }

}