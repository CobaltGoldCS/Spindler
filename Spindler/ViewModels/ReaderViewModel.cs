using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Reader_Pages;
using System.Xml.XPath;

namespace Spindler.ViewModels
{
    public partial class ReaderViewModel : ObservableObject, IReader, IRecipient<BookmarkClickedMessage>
    {
        #region Class Attributes
        // This should be set in ReaderPage.xaml.cs
        public ReaderDataService ReaderService = new(new Config(), new StandardWebService(new()));
        public IDataService Database;

        HttpClient Client;
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
        public void Bookmark()
        {
            if (CurrentData is null || IsLoading)
                return;
            Popup view = new BookmarkDialog(
                Database, 
                CurrentBook, 
                getNewBookmark: () => new Bookmark(CurrentData.Title!, ReaderScrollPosition, CurrentData.currentUrl!)
            );

            WeakReferenceMessenger.Default.Send(new CreateBottomSheetMessage(view));
        }

        public async void Receive(BookmarkClickedMessage message)
        {
            if (message.Value is null)
                return;
            var bookmark = message.Value;
            // UI Changes
            IsLoading = true;
            PrevButtonIsVisible = false;
            NextButtonIsVisible = false;

            // Preloaded Data is no longer valid for the bookmark
            ReaderService.InvalidatePreloadedData();

            await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
            var data = await ReaderService.LoadUrl(bookmark!.Url);
            switch (data)
            {
                case Invalid<LoadedData> error:
                    await SafeAssert(false, error.Value.getMessage());
                    return;
                case Ok<LoadedData> value:
                    CurrentData = value!.Value;
                    break;
            };

            // Cleanup 
            IsLoading = false;
            DataChanged();
            if (bookmark.Position > 0)
                await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, bookmark.Position, false);
            CurrentBook = await Database.GetItemByIdAsync<Book>(CurrentBook.Id);
        }

        [RelayCommand]
        public async void ScrollBottom()
        {
            await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, ReadingLayout.Content.Height - ReadingLayout.Bounds.Bottom, true);
        }
        #endregion

        #region Initialization Functions
        public ReaderViewModel(IDataService database, HttpClient client)
        {
            IsLoading = true;
            Shell.Current.Navigating += OnShellNavigating;
            Database = database;
            RegisterMessageHandlers();
            Client = client;
        }

        public ReaderViewModel SetRequiredInfo(ReaderDataService readerService)
        {
            ReaderService = readerService;
            return this;
        }

        public ReaderViewModel SetCurrentBook(Book book)
        {
            CurrentBook = book;
            return this;
        }

        public async Task StartLoad()
        {
            NextChapterService chapterService = new(Client);
            await CurrentBook.UpdateViewTimeAndSave(Database);
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
        public ReaderViewModel SetReferencesToUI(ScrollView readingLayout, Button bookmarkButton)
        {
            ReadingLayout = readingLayout;
            BookmarkButton = bookmarkButton;
            return this;
        }

        private void RegisterMessageHandlers()
        {
            WeakReferenceMessenger.Default.Register(this);
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
            await CurrentBook.UpdateViewTimeAndSave(Database);

            NextButtonIsVisible = WebUtilities.IsUrl(CurrentData.nextUrl);
            PrevButtonIsVisible = WebUtilities.IsUrl(CurrentData.prevUrl);

            // Turn relative uris into absolutes
            var baseUri = new Uri(CurrentData.currentUrl!);
            CurrentData.prevUrl = new Uri(baseUri: baseUri, CurrentData.prevUrl).ToString();
            CurrentData.nextUrl = new Uri(baseUri: baseUri, CurrentData.nextUrl).ToString();

            IsLoading = false;
            
            if (CurrentBook.Position > 0 && ReadingLayout is not null)
            {
                await ReadingLayout!.ScrollToAsync(
                        0,
                        CurrentBook.Position,
                        ReaderService.Config.HasAutoscrollAnimation);
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
