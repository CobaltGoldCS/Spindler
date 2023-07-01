using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.ViewModels;
using Spindler.Views;

namespace Spindler;

public partial class ReaderPage : ContentPage, IQueryAttributable
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
            .SetReferencesToUI(ReadingLayout, BookmarkItem)
            .SetCurrentBook(book)
            .Build();

        BindingContext = ViewModel;
        ReadingLayout.Scrolled += ViewModel.Scrolled;
        await ViewModel.StartLoad();
    }

    public ReaderPage(HttpClient client, IDataService dataService)
    {
        InitializeComponent();
        Client = client;
        DataService = dataService;

        WeakReferenceMessenger.Default.Register(this, async (object targetView, CreateBottomSheetMessage message) =>
        {
            var bookmark = await this.ShowPopupAsync(message.Value) as Bookmark;
            WeakReferenceMessenger.Default.Send(new BookmarkClickedMessage(bookmark!));
        });
    }
}