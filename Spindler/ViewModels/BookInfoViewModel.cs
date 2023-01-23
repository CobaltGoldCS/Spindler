using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Book_Pages;

namespace Spindler.ViewModels
{
    public partial class BookInfoViewModel : ObservableObject
    {
        public BookInfoViewModel() { }

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


        public void Load(Book book) => Task.Run(async () =>
        {
            this.book = book;
            book.FindConfig();
            configIsValid = book.Config is not null;
            if (!configIsValid) return;

            webview = book.Config?.ExtraConfigs.GetOrDefault("webview", false) ?? false;
            headless = book.Config?.ExtraConfigs.GetOrDefault("headless", false) ?? false;

            if (headless) Method = "Headless Reader";
            if (webview) Method = "Web View Reader";

            Title = book.Title;
            ImageUrl = book.ImageUrl;

            Domain = book.Config!.DomainName;
            TitleSelectorType = GetPathAsString(book.Config.TitlePath);
            ContentSelectorType = GetPathAsString(book.Config.ContentPath);
            PreviousSelectorType = GetPathAsString(book.Config.PrevUrlPath);
            NextSelectorType = GetPathAsString(book.Config.NextUrlPath);
        });

        [RelayCommand]
        public async void ReadClicked()
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book! }
            };

            string pageName = nameof(ReaderPage);
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
