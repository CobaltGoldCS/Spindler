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
        public ReaderDataService ReaderService = new ReaderDataService(new Config(), new StandardWebService());

        public Book CurrentBook = new() { Title = "Loading" };

        private ImageButton? BookmarkButton;
        private ScrollView? ReadingLayout;

        /// <summary>
        /// Task that should hold an array of length 2 containing (previous chapter, next chapter) in that order
        /// </summary>
        private Task<IResult<LoadedData>[]>? PreloadDataTask;

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
        private async void ChangeChapter(ConfigService.Selector selector)
        {
            IsLoading = true;
            await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
            CurrentBook.Url = selector switch
            {
                ConfigService.Selector.NextUrl => CurrentData!.nextUrl,
                ConfigService.Selector.PrevUrl => CurrentData!.prevUrl,
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
            CurrentBook = book;
        }
        public async Task StartLoad()
        {

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

                CurrentBook.ImageUrl = ReaderService.ConfigService.PrettyWrapSelector(
                    (html as Ok<string>)!.Value, ConfigService.Selector.ImageUrl, ConfigService.SelectorType.Link);
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
            // Database updates
            CurrentBook!.Url = CurrentData!.currentUrl!;
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

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await (ReadingLayout?.ScrollToAsync(
                        0,
                        CurrentBook.Position,
                        ReaderService.Config.HasAutoscrollAnimation) ?? Task.CompletedTask);
                });

            });
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
