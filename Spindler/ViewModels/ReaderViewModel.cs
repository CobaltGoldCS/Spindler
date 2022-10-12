using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utils;
using Spindler.Views;
using System.Net;
using System.Windows.Input;
namespace Spindler.ViewModels
{
    public partial class ReaderViewModel : ObservableObject
    {
        #region Class Attributes
        private WebService? _webservice = null;
        private WebService webService
        {
            get
            {
                _webservice ??= new(Config!);
                return _webservice;
            }
        }

        public Book? CurrentBook;
        public Config? Config { get; set; }

        /// <summary>
        /// Task that should hold an array of length 2 containing (previous chapter, next chapter) in that order
        /// </summary>
        private Task<LoadedData?[]>? PreloadDataTask;

        #region Bindable Properties

        [ObservableProperty]
        public LoadedData? loadedData;

        [ObservableProperty]
        private string title = "Loading";
        [ObservableProperty]
        private string text = "Content is currently loading";


        readonly bool defaultvisible = false;
        public bool PrevButtonIsVisible {
            get
            {
                if (loadedData is not null)
                    return WebService.IsUrl(loadedData.prevUrl);
                else
                    return defaultvisible;
            }
        }
        public bool NextButtonIsVisible {
            get
            {
                if (loadedData is not null)
                    return WebService.IsUrl(loadedData.nextUrl);
                else
                    return defaultvisible;
            }
        }
        #endregion
        #endregion

        #region Command Definitions
        public ICommand NextClickHandler { get; private set; }
        public ICommand PrevClickHandler { get; private set; }
        public ICommand BookmarkCommand  { get; private set; }
        #endregion

        #region Initialization Functions
        public ReaderViewModel()
        {
            #region Command Implementations
            PrevClickHandler = new Command(async () =>
            {
                var prevdata = (await PreloadDataTask!)![0];
                if (!await FailIfNull(prevdata, "Invalid Url"))
                {
                    loadedData = prevdata;
                    DataChanged();
                    await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                }
            });
            NextClickHandler = new Command(async () =>
            {
                var nextdata = (await PreloadDataTask!)![1];
                if (!await FailIfNull(nextdata, "Invalid Url"))
                {
                    loadedData = nextdata;
                    DataChanged();
                    await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                }
            });
            BookmarkCommand = new Command(async () =>
            {
                double prevbuttonheight = PrevButtonIsVisible ? ButtonHeight : 0;
                double nextbuttonheight = NextButtonIsVisible ? ButtonHeight : 0;
                await App.Database.SaveItemAsync<Book>(new()
                {
                    BookListId = CurrentBook!.BookListId,
                    Id = -1,
                    Title = "Bookmark: " + loadedData!.title,
                    Url = loadedData.currentUrl!,
                    Position = ReadingLayout!.ScrollY / (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight)),
                    LastViewed = DateTime.UtcNow,
                });
            });
            #endregion
        }

        public async Task StartLoad()
        {
            if (await FailIfNull(Config, "Configuration does not exist")) return;
            LoadedData? data = await webService.LoadUrl(CurrentBook!.Url);
            if (await FailIfNull(data, "Invalid Url")) return;
            loadedData = data!;
            DataChanged();
            await DelayScroll();
        }

        private ScrollView? ReadingLayout;
        public double ButtonHeight { get; set; }

        public void AttachReferencesToUI(ScrollView readingLayout, double buttonheight)
        {
            ButtonHeight = buttonheight;
            ReadingLayout = readingLayout;
        }
        #endregion

        public void ClearCookies() => WebService.ClearCookies();

        #region Helperfunctions

        private async void DataChanged()
        {
            if (await FailIfNull(loadedData, "This is an invalid url")) return;
            if (loadedData!.title == "afb-4893") // This means an error has occured while getting data from the WebService
            {
                await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}?id={CurrentBook!.Id}&errormessage={loadedData.text}");
                return;
            }
            // Database updates
            CurrentBook!.Url = loadedData.currentUrl!;
            CurrentBook.LastViewed = DateTime.UtcNow;
            await App.Database.SaveItemAsync(CurrentBook);

            Title = loadedData.title ?? "";
            Text = loadedData.text ?? "";

            OnPropertyChanged(nameof(NextButtonIsVisible));
            OnPropertyChanged(nameof(PrevButtonIsVisible));

            PreloadDataTask = webService.LoadData(loadedData.prevUrl, loadedData.nextUrl);
        }


        private async Task DelayScroll()
        {
            await Task.Run(async () =>
            {
                double prevbuttonheight = PrevButtonIsVisible ? ButtonHeight : 0;
                double nextbuttonheight = NextButtonIsVisible ? ButtonHeight : 0;
                await Task.Delay(100);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX,
                        Math.Clamp(CurrentBook!.Position, 0d, 1d) * (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight)),
                        (bool)Config!.ExtraConfigs.GetOrDefault("autoscrollanimation", true));
                });
                
            });
        }
        #region Error Handlers
        /// <summary>
        /// Display <paramref name="message"/> if <paramref name="value"/> is <c>null</c>
        /// </summary>
        /// <param name="value">The value to check for nullability</param>
        /// <param name="message">The value to display</param>
        /// <returns>If the object is null or not</returns>
        private async Task<bool> FailIfNull(object? value, string message)
        {
            bool nullobj = value == null;
            if (nullobj)
            {
                await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}?id={CurrentBook!.Id}&errormessage={message}");
            }
            return nullobj;
        }
        #endregion
        #endregion
    }
}
