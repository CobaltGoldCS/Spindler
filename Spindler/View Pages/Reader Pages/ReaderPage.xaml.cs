using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;

namespace Spindler;

public partial class ReaderPage : ContentPage, IQueryAttributable
{
    HttpClientHandler ClientHandler { get; set; }
    public enum ReaderType
    {
        Standard,
        Headless
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book book = (query["book"] as Book)!;
        
        ReaderType type = (ReaderType)query["type"];
        if (!query.TryGetValue("config", out object? config))
        {
            config = await Config.FindValidConfig(book.Url);
        }

        var ViewModel = new ReaderViewModel();
        // Attach required information to View Model
        if (config is not null)
        {
            ClientHandler.AllowAutoRedirect = true;
            ClientHandler.PreAuthenticate = true;
            HttpClient client = new(ClientHandler);
            ViewModel.SetRequiredInfo(new((Config)config, type switch
            {
                ReaderType.Headless => new HeadlessWebService(HeadlessBrowser),
                ReaderType.Standard => new StandardWebService(client),
                _ => throw new NotImplementedException("This service has not been implemented")
            }));
        }
        ViewModel.SetReferencesToUI(ReadingLayout, BookmarkItem);
        ViewModel.SetCurrentBook(book);
        ReadingLayout.Scrolled += ViewModel.Scrolled;

        BindingContext = ViewModel;
        await ViewModel.StartLoad();
    }

    public ReaderPage(HttpClientHandler handler)
    {
        InitializeComponent();
        ClientHandler = handler;
    }
}