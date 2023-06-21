using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Reader_Pages;
using System.Xml.XPath;

namespace Spindler.ViewModels
{
    public partial class ReaderViewModel : ObservableObject, IReader
    {
        #region Class Attributes
        // This should be set in ReaderPage.xaml.cs
        public ReaderDataService ReaderService = new(new Config(), new StandardWebService(new()));

        public Book CurrentBook = new() { Title = "Loading" };
        public CancellationTokenRegistration nextChapterToken = new();

        private Button? BookmarkButton;
        private ScrollView? ReadingLayout;

        #region Bindable Properties

        [ObservableProperty]
        public LoadedData? currentData = new() { Title = "Loading" };

        [ObservableProperty]
        private bool isLoading;


        [ObservableProperty]
        public bool prevButtonIsVisible = false;
        [ObservableProperty]
        public bool nextButtonIsVisible = false;

        double ReaderScrollPosition = 0;

        public async void Scrolled(object? sender, ScrolledEventArgs e)
        {
            await Task.Run(() =>
            {
                ReaderScrollPosition = e.ScrollY;
                if (ReaderScrollPosition.IsZeroOrNaN())
                    return;

                CurrentBook.Position = ReaderScrollPosition;
            });
        }
        #endregion
        #endregion

        #region Command Definitions

        [RelayCommand]
        private async void ChangeChapter(ReaderDataService.UrlType selector)
        {
            if (IsLoading)
                return;
            
            IsLoading = true;
            PrevButtonIsVisible = false;
            NextButtonIsVisible = false;
            await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
            CurrentBook.Position = 0;

            CurrentBook.Url = selector switch
            {
                ReaderDataService.UrlType.Next => CurrentData!.nextUrl,
                ReaderDataService.UrlType.Previous => CurrentData!.prevUrl,
                _ => throw new InvalidDataException("Invalid value for selector; selector must be prev or next url")
            };

            var dataResult = await ReaderService.GetLoadedData(selector, CurrentData!);
            if(dataResult is Invalid<LoadedData> error)
            {
                await SafeAssert(false, error.Value.getMessage());
                return;
            }
            CurrentData = (dataResult as Ok<LoadedData>)!.Value;
            DataChanged();
        }

        [RelayCommand]
        public async void Bookmark()
        {
            await BookmarkButton!.RelRotateTo(360, 250, Easing.CubicIn);
            await App.Database.SaveItemAsync(CurrentBook with
            {
                Title = "Bookmark: " + CurrentData!.Title,
                Id = -1,
                LastViewed = DateTime.UtcNow,
                Pinned = false
            });
        }

        [RelayCommand]
        public async void ScrollBottom()
        {
            await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, ReadingLayout.Content.Height - ReadingLayout.Bounds.Bottom, true);
        }
        #endregion

        #region Initialization Functions
        public ReaderViewModel()
        {
            IsLoading = true;
            Shell.Current.Navigating += OnShellNavigating;
        }
        public void SetRequiredInfo(ReaderDataService readerService)
        {
            ReaderService = readerService;
        }
        public void SetCurrentBook(Book book)
        {
            CurrentBook = book;
        }
        public async Task StartLoad()
        {
            NextChapterService chapterService = new();
            await CurrentBook.UpdateViewTimeAndSave();
            IDispatcher? dispatcher = Dispatcher.GetForCurrentThread();
            await Task.Run(async () =>
            {
                IEnumerable<Book> updateQueue = await chapterService.CheckChaptersInBookList(CurrentBook, nextChapterToken.Token);
                await dispatcher!.DispatchAsync(async () => await App.Database.SaveItemsAsync(updateQueue));
            });

            var data = await ReaderService.LoadUrl(CurrentBook!.Url);
            switch (data)
            {
                case Invalid<LoadedData> error:
                    await SafeAssert(false, error.Value.getMessage());
                    return;
                case Ok<LoadedData> value:
                    CurrentData = value!.Value;
                    break;
            };

            // Get image url from load
            if (string.IsNullOrEmpty(CurrentBook!.ImageUrl) || CurrentBook!.ImageUrl == "no_image.jpg")
            {
                var html = await ReaderService.WebService.GetHtmlFromUrl(CurrentBook.Url);

                if (html is Invalid<string>) return;

                try
                {
                    CurrentBook.ImageUrl = ReaderService.ConfigService.PrettyWrapSelector(
                                        (html as Ok<string>)!.Value, ConfigService.Selector.ImageUrl, ConfigService.SelectorType.Link);
                }
                catch (XPathException) { }
            }
            DataChanged();
        }
        public void SetReferencesToUI(ScrollView readingLayout, Button bookmarkButton)
        {
            ReadingLayout = readingLayout;
            BookmarkButton = bookmarkButton;
        }
        #endregion

        public async void OnShellNavigating(object? sender,
                           ShellNavigatingEventArgs e)
        {
            if (e.Target.Location.OriginalString == "..")
            {
                CurrentBook.HasNextChapter = NextButtonIsVisible;
                await App.Database.SaveItemAsync(CurrentBook);
            }
            Shell.Current.Navigating -= OnShellNavigating;
        }

        #region Helperfunctions

        private async void DataChanged()
        {
            // Database updates
            CurrentBook!.Url = CurrentData!.currentUrl!;
            await CurrentBook.UpdateViewTimeAndSave();

            NextButtonIsVisible = WebUtilities.IsUrl(CurrentData.nextUrl);
            PrevButtonIsVisible = WebUtilities.IsUrl(CurrentData.prevUrl);

            // Turn relative uris into absolutes
            var baseUri = new Uri(CurrentData.currentUrl!);
            CurrentData.prevUrl = new Uri(baseUri: baseUri, CurrentData.prevUrl).ToString();
            CurrentData.nextUrl = new Uri(baseUri: baseUri, CurrentData.nextUrl).ToString();

            IsLoading = false;
            
            if (CurrentBook.Position > 0)
            {
                await (ReadingLayout?.ScrollToAsync(
                        0,
                        CurrentBook.Position,
                        ReaderService.Config.HasAutoscrollAnimation) ?? Task.CompletedTask);
            }
        }


        #region Error Handlers

        public async Task<bool> SafeAssert(bool condition, string message)
        {
            if (condition)
                return true;

            Dictionary<string, object?> parameters = new()
            {
                { "errormessage", message },
                { "config", ReaderService.Config }
            };
            await Shell.Current.GoToAsync($"{nameof(ErrorPage)}", parameters);
            return false;
        }

        #endregion
        #endregion
    }
}
