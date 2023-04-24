using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Views;
using Spindler.Views.Book_Pages;

namespace Spindler.ViewModels
{
    public partial class BookViewModel : ObservableObject
    {
        public BookViewModel() { }

        Book? book;

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
        string method = "Normal Reader";

        Config? config;


        public async Task Load(Book book)
        {
            this.book = book;
            Domain = new UriBuilder(book.Url).Host;

            config = await Config.FindValidConfig(book.Url);

            Title = book.Title;
            ImageUrl = book.ImageUrl;

            if (config is null)
            {
                // This is for general configurations where the FindValidConfig call may fail due to a 403 forbidden or the sort
                config = new Config() { UsesHeadless = true };
                Method = "Headless Reader";
                return;
            }

            if (config!.UsesHeadless) Method = "Headless Reader";
            if (config!.UsesWebview) Method = "Web View Reader";

            TitleSelectorType = GetPathAsString(config.TitlePath);
            ContentSelectorType = GetPathAsString(config.ContentPath);
            PreviousSelectorType = GetPathAsString(config.PrevUrlPath);
            NextSelectorType = GetPathAsString(config.NextUrlPath);
        }

        [RelayCommand]
        public async void ReadClicked()
        {
            Dictionary<string, object?> parameters = new()
            {
                { "book", book! },
                { "config", config }
            };

            string pageName = nameof(StandardReaderPage);
            if (config!.UsesWebview)
            {
                pageName = nameof(WebviewReaderPage);
            }
            if (config.UsesHeadless)
            {
                pageName = nameof(HeadlessReaderPage);
            }
            await Shell.Current.GoToAsync($"../{pageName}", parameters);
            return;
        }

        [RelayCommand]
        public async void ModifyClicked()
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book! }
            };
            await Shell.Current.GoToAsync($"../{nameof(BookDetailPage)}", parameters: parameters);
        }

        private static string GetPathAsString(string path)
        {
            var tempPath = new Models.Path(path);
            return tempPath.type switch
            {
                Models.Path.Type.XPath => "X Path",
                Models.Path.Type.Css => "CSS Path",
                _ => "Unknown",
            };
        }

    }
}
