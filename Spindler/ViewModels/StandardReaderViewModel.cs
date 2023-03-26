using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Reader_Pages;
namespace Spindler.ViewModels
{
    public partial class StandardReaderViewModel : ObservableObject, IReader
    {
        #region Class Attributes
        private ReaderDataService? readerService = null;
        public ReaderDataService ReaderService
        {
            get
            {
                // This should not deadlock, but if the page does, its probably this line
                if (!Task.Run(async() => await SafeAssertNotNull(readerService, "Configuration does not exist")).Result)
                {
                    return new ReaderDataService(new Config());
                }
                return readerService!;
            }
            set => readerService = value;
        }

        public Book CurrentBook = new() { Title = "Loading" };

        /// <summary>
        /// Task that should hold an array of length 2 containing (previous chapter, next chapter) in that order
        /// </summary>
        private Task<LoadedData?[]>? PreloadDataTask;

        #region Bindable Properties

        [ObservableProperty]
        public LoadedData? currentData = new() { Title = "Loading" };

        [ObservableProperty]
        private bool isLoading;


        readonly bool defaultvisible = false;
        public bool PrevButtonIsVisible
        {
            get
            {
                if (CurrentData is not null)
                    return WebService.IsUrl(CurrentData.prevUrl);
                else
                    return defaultvisible;
            }
        }
        public bool NextButtonIsVisible
        {
            get
            {
                if (CurrentData is not null)
                    return WebService.IsUrl(CurrentData.nextUrl);
                else
                    return defaultvisible;
            }
        }

        double ReaderScrollPosition = 0;
        double ReaderHeight = 0;

        // FIXME: This resets ReaderScrollPosition and ReaderHeight to 0 when 
        // leaving the page. That is why the last line is there
        public async void Scrolled(object sender, ScrolledEventArgs e)
        {
            var scrollView = (ScrollView)sender;
            if (scrollView.ScrollY > 0)
            {
                ReaderScrollPosition = scrollView.ScrollY;
            }
            if (scrollView.ContentSize.Height > 0)
            {
                ReaderHeight = scrollView.ContentSize.Height;
            }
            await Task.Run(() => {
                var scrollPercentage = ReaderScrollPosition / ReaderHeight;
                if (scrollPercentage.IsZeroOrNaN())
                    return;
                CurrentBook.Position = scrollPercentage;
            });
        }
        #endregion
        #endregion

        #region Command Definitions

        [RelayCommand]
        public async void NextClick()
        {
            var nextdata = (await PreloadDataTask!)![1];
            if (await SafeAssertNotNull(nextdata, "Invalid Url"))
            {
                await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                IsLoading = true;
                CurrentData = nextdata;
                DataChanged();
            }
        }

        [RelayCommand]
        public async void PrevClick()
        {
            var prevdata = (await PreloadDataTask!)![0];
            if (await SafeAssertNotNull(prevdata, "Invalid Url"))
            {
                await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                IsLoading = true;
                CurrentData = prevdata;
                DataChanged();
            }
        }

        [RelayCommand]
        public async void Bookmark()
        {
            await BookmarkButton!.RelRotateTo(360, 250, Easing.CubicIn);
            await App.Database.SaveItemAsync<Book>(new()
            {
                BookListId = CurrentBook!.BookListId,
                Title = "Bookmark: " + CurrentData!.Title,
                Url = CurrentData.currentUrl!,
                Position = ReadingLayout!.ScrollY / ReadingLayout.ContentSize.Height,
            });
        }
        #endregion

        #region Initialization Functions
        public StandardReaderViewModel()
        { 
            IsLoading = true;
            Shell.Current.Navigating += OnShellNavigated;
        }
        public void LoadRequiredInfo(ReaderDataService readerService)
        {
            ReaderService = readerService;
        }
        CancellationTokenSource tokenRegistration = new CancellationTokenSource();
        public async Task StartLoad()
        {
            // Start background chapter checker service
            var mainThread = Dispatcher.GetForCurrentThread();
            var service = new NextChapterService();
            await Task.Run(async () =>
            {
                var updateQueue = await service.CheckChaptersInBookList(CurrentBook!.BookListId, tokenRegistration.Token);
                await mainThread!.DispatchAsync(async () => await App.Database.SaveItemsAsync(updateQueue));
            });

            LoadedData? data = await ReaderService.LoadUrl(CurrentBook!.Url);

            if (!await SafeAssertNotNull(data, "Invalid Url"))
                return;

            CurrentData = data!;
            // Get image url from load
            if (string.IsNullOrEmpty(CurrentBook!.ImageUrl) || CurrentBook!.ImageUrl == "no_image.jpg")
            {
                var html = await ReaderService.WebService.HtmlOrError(CurrentBook.Url);
                
                if (Result.IsError(html)) return;

                CurrentBook.ImageUrl = ConfigService.PrettyWrapSelector(html.AsOk().Value, new Models.Path(ReaderService.Config.ImageUrlPath), ConfigService.SelectorType.Link);
            }
            DataChanged();
            await DelayScroll();
        }

        private ImageButton? BookmarkButton;
        private ScrollView? ReadingLayout;
        public void AttachReferencesToUI(ScrollView readingLayout, ImageButton bookmarkButton)
        {
            ReadingLayout = readingLayout;
            BookmarkButton = bookmarkButton;
        }
        #endregion

        #region Helperfunctions

        private async void DataChanged()
        {
            if (!await SafeAssertNotNull(CurrentData, "This is an invalid url"))
                return;
            if (!await SafeAssert(CurrentData!.Title != "afb-4893", CurrentData.Text!)) // This means an error has occurred while getting data from the WebService
                return;
            // Database updates
            CurrentBook!.Url = CurrentData.currentUrl!;
            await CurrentBook.UpdateViewTimeAndSave();

            OnPropertyChanged(nameof(NextButtonIsVisible));
            OnPropertyChanged(nameof(PrevButtonIsVisible));

            PreloadDataTask = ReaderService.LoadData(CurrentData.prevUrl, CurrentData.nextUrl);
            IsLoading = false;
        }


        private async Task DelayScroll()
        {
            await Task.Run(async () =>
            {
                await Task.Delay(100);

  
                var hasAutoScrollAnimation = (bool)ReaderService.Config.ExtraConfigs.GetValueOrDefault("autoscrollanimation", true);
                
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await (ReadingLayout?.ScrollToAsync(
                        ReaderScrollPosition,
                        ReaderHeight,
                        hasAutoScrollAnimation) ?? Task.CompletedTask);
                });

            });
        }

        public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
        {
            if (e.Target.Location.OriginalString == "..")
            {
                CurrentBook.HasNextChapter = NextButtonIsVisible;
                await App.Database.SaveItemAsync(CurrentBook);
            }
            tokenRegistration.Cancel();
            Shell.Current.Navigating -= OnShellNavigated;
        }

        #region Error Handlers
        public async Task<bool> SafeAssertNotNull(object? value, string message)
            => await SafeAssert(value != null, message);

        public async Task<bool> SafeAssert(bool condition, string message)
        {
            if (condition)
                return true;

            Dictionary<string, object> parameters = new()
            {
                { "errormessage", message },
                { "config", ReaderService.Config }
            };
            await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}", parameters);
            return false;
        }

        #endregion
        #endregion
    }
}
