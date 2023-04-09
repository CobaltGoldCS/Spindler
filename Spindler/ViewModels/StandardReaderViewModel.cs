using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        CancellationTokenSource tokenRegistration = new CancellationTokenSource();
        private ReaderDataService? readerService = null;
        public ReaderDataService ReaderService
        {
            get
            {
                // This should not deadlock, but if the page does, its probably this line
                if (!Task.Run(async () => await SafeAssertNotNull(readerService, "Configuration does not exist")).Result)
                {
                    return new ReaderDataService(new Config(), new StandardWebService());
                }
                return readerService!;
            }
            set => readerService = value;
        }

        public Book CurrentBook = new() { Title = "Loading" };

        private ImageButton? BookmarkButton;
        private ScrollView? ReadingLayout;

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
                    return WebUtilities.IsUrl(CurrentData.prevUrl);
                else
                    return defaultvisible;
            }
        }
        public bool NextButtonIsVisible
        {
            get
            {
                if (CurrentData is not null)
                    return WebUtilities.IsUrl(CurrentData.nextUrl);
                else
                    return defaultvisible;
            }
        }

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
        }
        public void SetRequiredInfo(ReaderDataService readerService)
        {
            ReaderService = readerService;
        }
        public void SetCurrentBook(Book book)
        {
            this.CurrentBook = book;
        }
        public async Task StartLoad()
        {

            LoadedData? data = await ReaderService.LoadUrl(CurrentBook!.Url);

            if (!await SafeAssertNotNull(data, "Invalid Url"))
                return;

            CurrentData = data!;
            // Get image url from load
            if (string.IsNullOrEmpty(CurrentBook!.ImageUrl) || CurrentBook!.ImageUrl == "no_image.jpg")
            {
                var html = await ReaderService.WebService.GetHtmlFromUrl(CurrentBook.Url);

                if (Result.IsError(html)) return;

                CurrentBook.ImageUrl = ConfigService.PrettyWrapSelector(html.AsOk().Value, new Models.Path(ReaderService.Config.ImageUrlPath), ConfigService.SelectorType.Link);
            }
            DataChanged();
            await DelayScroll();
        }
        public void SetReferencesToUI(ScrollView readingLayout, ImageButton bookmarkButton)
        {
            ReadingLayout = readingLayout;
            BookmarkButton = bookmarkButton;
        }
        #endregion

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
                if (CurrentBook.Position <= 0)
                    return;

                await Task.Delay(100);


                var hasAutoScrollAnimation = (bool)ReaderService.Config.ExtraConfigs.GetValueOrDefault("autoscrollanimation", true);

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await (ReadingLayout?.ScrollToAsync(
                        0,
                        CurrentBook.Position,
                        hasAutoScrollAnimation) ?? Task.CompletedTask);
                });

            });
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
                { "config", readerService?.Config }
            };
            await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}", parameters);
            return false;
        }

        #endregion
        #endregion
    }
}
