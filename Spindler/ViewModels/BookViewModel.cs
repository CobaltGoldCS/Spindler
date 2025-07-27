using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Views.Book_Pages;
using Spindler.Views.Reader_Pages;

namespace Spindler.ViewModels;

public partial class BookViewModel(IDataService database, HttpClient client) : SpindlerViewModel(database)
{
    readonly HttpClient Client = client;

    Book? book;
    Config? config;
    [ObservableProperty]
    Brush background = new SolidColorBrush(Application.Current?.Resources["CardBackground"] as Color ?? Colors.CadetBlue);

    [ObservableProperty]
    string imageUrl = "";

    [ObservableProperty]
    string title = "Loading...";

    [ObservableProperty]
    bool configIsValid = false;

    [ObservableProperty]
    string domain = "Unknown";

    [ObservableProperty]
    string titleSelectorType = "Unknown";

    [ObservableProperty]
    string contentSelectorType = "Unknown";

    [ObservableProperty]
    string previousSelectorType = "Unknown";

    [ObservableProperty]
    string nextSelectorType = "Unknown";

    [ObservableProperty]
    string method = "Read With Normal Reader";


    public async Task Load(Book book)
    {
        this.book = book;
        Domain = new UriBuilder(book.Url).Host;

        config = await Config.FindValidConfig(Database, Client, book.Url);

        Title = book.Title;
        ImageUrl = book.ImageUrl;

        if (config is null)
        {
            // This is for general configurations where the FindValidConfig call may fail due to a 403 forbidden or the sort
            config = new Config() { UsesHeadless = true };
            Method = "Headless Reader";
            return;
        }

        if (config!.UsesHeadless) Method = "Read With Headless Reader";
        if (config!.UsesWebview) Method = "Read With Web View Reader";

        TitleSelectorType = GetPathTypeAsString(config.TitlePath);
        ContentSelectorType = GetPathTypeAsString(config.ContentPath);
        PreviousSelectorType = GetPathTypeAsString(config.PrevUrlPath);
        NextSelectorType = GetPathTypeAsString(config.NextUrlPath);
    }
    [RelayCommand]
    public async Task ReadClicked()
    {
        Dictionary<string, object?> parameters = new()
        {
            { "book", book! },
            { "config", config },
            { "type", config?.UsesHeadless ?? true ? ReaderPage.ReaderType.Headless : ReaderPage.ReaderType.Standard }
        };

        string pageName = nameof(ReaderPage);
        if (config?.UsesWebview ?? false)
        {
            pageName = nameof(WebviewReaderPage);
        }
        await NavigateTo($"../{pageName}", parameters);
        return;
    }

    [RelayCommand]
    public async Task ModifyClicked()
    {
        Dictionary<string, object> parameters = new()
    {
        { "book", book! }
    };
        await NavigateTo($"../{nameof(BookDetailPage)}", parameters: parameters);
    }

    private static string GetPathTypeAsString(string path)
    {
        return path.AsPath().PathType switch
        {
            Models.Path.Type.XPath => "X Path",
            Models.Path.Type.Css => "CSS Path",
            _ => "Unknown",
        };
    }

}
