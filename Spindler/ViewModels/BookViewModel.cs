using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Book_Pages;

namespace Spindler.ViewModels
{
    public partial class BookViewModel : ObservableObject
    {
        public BookViewModel() { }

        Book? book;

        [ObservableProperty]
        string imageUrl = "no_image.jpg";

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

        bool webview = false;
        bool headless = false;


        public async Task Load(Book book)
        {
            this.book = book;
            Domain = new UriBuilder(book.Url).Host;

            Config? config = await Config.FindValidConfig(book.Url);
            
            Title = book.Title;
            ImageUrl = book.ImageUrl;
            
            if (config is null)
            {
                // This is for general configurations where the FindValidConfig call may fail due to a 403 forbidden or the sort
                headless = true;
                Method = "Headless Reader";
                return;
            }

            webview = (bool)config!.ExtraConfigs.GetValueOrDefault("webview", false);
            headless = (bool)config.ExtraConfigs.GetValueOrDefault("headless", false);

            if (headless) Method = "Headless Reader";
            if (webview) Method = "Web View Reader";

            TitleSelectorType = GetPathAsString(config.TitlePath);
            ContentSelectorType = GetPathAsString(config.ContentPath);
            PreviousSelectorType = GetPathAsString(config.PrevUrlPath);
            NextSelectorType = GetPathAsString(config.NextUrlPath);
        }

        [RelayCommand]
        public async void ReadClicked()
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book! }
            };

            string pageName = nameof(StandardReaderPage);
            if (webview)
            {
                pageName = nameof(WebviewReaderPage);
            }
            if (headless)
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
