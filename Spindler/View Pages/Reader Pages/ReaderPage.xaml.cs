using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;

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
        if (!query.TryGetValue("config", out object? config))
        {
            config = await Config.FindValidConfig(book.Url);
        }

        var ViewModel = new ReaderViewModel(DataService);
        // Attach required information to View Model
        if (config is not null)
        {
            ViewModel.SetRequiredInfo(new((Config)config, type switch
            {
                ReaderType.Headless => new HeadlessWebService(HeadlessBrowser),
                ReaderType.Standard => new StandardWebService(Client),
                _ => throw new NotImplementedException("This service has not been implemented")
            }));
        }
        ViewModel.SetReferencesToUI(ReadingLayout, BookmarkItem);
        ViewModel.SetCurrentBook(book);
        ReadingLayout.Scrolled += ViewModel.Scrolled;

        BindingContext = ViewModel;
        await ViewModel.StartLoad();
    }

    public ReaderPage(HttpClient client, IDataService dataService)
    {
        InitializeComponent();
        Client = client;
        DataService = dataService;
    }
}