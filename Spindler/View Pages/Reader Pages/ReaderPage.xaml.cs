using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.ViewModels;
using Spindler.Views;
using System.ComponentModel;

namespace Spindler;

public partial class ReaderPage : ContentPage, IQueryAttributable, IRecipient<CreateBottomSheetMessage>, IRecipient<ChangeScrollMessage>
{
    HttpClient Client { get; set; }
    IDataService DataService { get; set; }

    public enum ReaderType
    {
        Standard,
        Headless
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book book = (query["book"] as Book)!;
        
        ReaderType type = (ReaderType)query["type"];
        if (!query.TryGetValue("config", out object? configObject))
        {
            configObject = await Config.FindValidConfig(Client, book.Url);
        }

        

        if (configObject is null)
        {
            await Toast.Make("Error: Couldn't find configuration").Show();
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.GoToAsync(".."));
            });
            return;
        }
        var ViewModel = new ReaderViewModelBuilder(DataService, Client)
            .SetRequiredInfo(new((Config)configObject, type switch
            {
                ReaderType.Headless => new HeadlessWebService(HeadlessBrowser),
                ReaderType.Standard => new StandardWebService(Client),
                _ => throw new NotImplementedException("This service has not been implemented")
            }))
            .SetCurrentBook(book)
            .Build();

        BindingContext = ViewModel;

        BookmarkItem.Behaviors.Add(new AnimationBehavior
        {
            AnimationType = new RotationAnimation(),
            Command = ViewModel.BookmarkCommand
        });

        await ViewModel.StartLoad();
    }

    async void IRecipient<CreateBottomSheetMessage>.Receive(CreateBottomSheetMessage message)
    {
        var bookmark = await this.ShowPopupAsync(message.Value) as Bookmark;
        WeakReferenceMessenger.Default.Send(new BookmarkClickedMessage(bookmark!));
    }

    /// <summary>
    /// In charge of scrolling to positions. NOTE: Negative values scroll to bottom
    /// </summary>
    /// <param name="message">A message containing position (double) and isAnimated (bool)</param>
    async void IRecipient<ChangeScrollMessage>.Receive(ChangeScrollMessage message)
    {
        if (message.Value.Item1 < 0)
        {
            await ReadingLayout.ScrollToAsync(ReaderView, ScrollToPosition.End, message.Value.Item2);
        } else
        {
            await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, message.Value.Item1, message.Value.Item2);
        }
    }



    public ReaderPage(HttpClient client, IDataService dataService)
    {
        InitializeComponent();
        Client = client;
        DataService = dataService;
        WeakReferenceMessenger.Default.Register<CreateBottomSheetMessage>(this);
        WeakReferenceMessenger.Default.Register<ChangeScrollMessage>(this);
    }
}

class RotationAnimation : BaseAnimation
{
    public async override Task Animate(VisualElement view)
    {
        await view.RelRotateTo(360, easing: Easing.CubicIn);
    }
}