using Spindler.Services;
using Spindler.Models;
using Spindler.Views;
using System.ComponentModel;
using System.Windows.Input;
using System.Runtime.CompilerServices;

namespace Spindler.ViewModels
{
    public class ReaderViewModel : INotifyPropertyChanged
    {
        #region Class Attributes
        private WebService _webservice = null;
        private WebService webService
        {
            get
            {
                if (_webservice is null)
                {
                   _webservice = new(Config);
                }
                return _webservice;
            }
        }

        public Book CurrentBook;
        public Config Config { get; set; }

        /// <summary>
        /// Task that should hold an array of length 2 containing (previous chapter, next chapter) in that order
        /// </summary>
        private Task<LoadedData[]> PreloadDataTask;

        #region Bindable Properties
        private LoadedData loadedData = new LoadedData 
        { 
            title = "Loading",
            text = "Content is currently loading"
        };
        public LoadedData LoadedData
        {
            get => loadedData;
            set
            {
                if (loadedData == value)
                    return;
                loadedData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PrevButtonIsVisible));
                OnPropertyChanged(nameof(NextButtonIsVisible));
            }
        }

        private string title = "Loading";
        public string Title
        {
            get => title;
            set
            {
                if (title == value)
                    return;
                title = value;
                OnPropertyChanged();
            }
        }
        private string text = "Content is currently loading";
        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;
                text = value;
                OnPropertyChanged();
            }
        }

        readonly bool defaultvisible = false;
        public bool PrevButtonIsVisible {
            get
            {
                if (LoadedData is not null)
                    return WebService.IsUrl(LoadedData.prevUrl);
                else
                    return defaultvisible;
            }
        }
        public bool NextButtonIsVisible {
            get
            {
                if (LoadedData is not null)
                    return WebService.IsUrl(LoadedData.nextUrl);
                else
                    return defaultvisible;
            }
        }

        private string prevText = "Previous";
        public string PrevText
        {
            get => prevText;
            set
            {
                if (prevText == value)
                    return;
                prevText = value;
                OnPropertyChanged();
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
#nullable enable
        public ReaderViewModel()
        {
            #region Command Implementations
            PrevClickHandler = new Command(async () =>
            {
                var prevdata = (await PreloadDataTask)[0];
                if (!FailIfNull(prevdata, "Invalid Url"))
                {
                    loadedData = prevdata;
                    DataChanged();
                    await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                }
            });
            NextClickHandler = new Command(async () =>
            {
                var nextdata = (await PreloadDataTask)[1];
                if (!FailIfNull(nextdata, "Invalid Url"))
                {
                    loadedData = nextdata;
                    DataChanged();
                    await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                }
            });
            BookmarkCommand = new Command(async () =>
            {
                double prevbuttonheight = PrevButtonIsVisible ? ButtonHeight : 0;
                double nextbuttonheight = NextButtonIsVisible ? ButtonHeight : 0;
                await App.Database.SaveItemAsync<Book>(new()
                {
                    BookListId = CurrentBook.BookListId,
                    Id = -1,
                    Title = "Bookmark: " + loadedData.title,
                    Url = loadedData.currentUrl,
                    Position = ReadingLayout.ScrollY / (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight)),
                    LastViewed = DateTime.UtcNow,
                });
            });
            #endregion
        }

        public async Task StartLoad()
        {
            if (FailIfNull(Config, "Configuration does not exist")) return;
            LoadedData data = await webService.LoadUrl(CurrentBook.Url);
            if (FailIfNull(data, "Invalid Url")) return;
            LoadedData = data;
            DataChanged();
            await DelayScroll();
        }
#nullable disable

        private ScrollView ReadingLayout;
        public double ButtonHeight { get; set; }
        public void AttachReferencesToUI(ScrollView readingLayout, double buttonheight)
        {
            ButtonHeight = buttonheight;
            ReadingLayout = readingLayout;
        }
        #endregion

        #region Helperfunctions

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void DataChanged()
        {
            if (FailIfNull(LoadedData, "This is an invalid url")) return;
            // Database updates
            CurrentBook.Url = LoadedData.currentUrl;
            CurrentBook.LastViewed = DateTime.UtcNow;
            await App.Database.SaveItemAsync(CurrentBook);

            Title = loadedData.title;
            Text = loadedData.text;

            PreloadDataTask = webService.LoadData(LoadedData.prevUrl, LoadedData.nextUrl);
            ScrollWorkaround();
        }

        private void ScrollWorkaround()
        {
            var content = ReadingLayout.Content;
            ReadingLayout.Content = null;
            ReadingLayout.Content = content;
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
                    await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, Math.Clamp(CurrentBook.Position, 0d, 1d) * (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight)), true);
                });
                
            });
        }
        #region Error Handlers
#nullable enable
        /// <summary>
        /// Display <paramref name="message"/> if <paramref name="value"/> is <c>null</c>
        /// </summary>
        /// <param name="value">The value to check for nullability</param>
        /// <param name="message">The message to display</param>
        /// <returns>If the object is null or not</returns>
        private bool FailIfNull(object? value, string message)
        {
            bool nullobj = value == null;
            if (nullobj)
            {
                LoadedData = MakeFailMessage(message);
                // Change Previous Chapter button to a button that navigates to the WebviewReaderPage
                prevText = "Open in Web View?";
                PrevClickHandler = new Command(async () =>
                {
                    await Shell.Current.GoToAsync($"../{nameof(WebviewReaderPage)}?id={CurrentBook.Id}");
                });
                DataChanged();
            }
            return nullobj;
        }

        private LoadedData MakeFailMessage(string message)
        {
            return new LoadedData
            {
                prevUrl = "https://failed.com",
                nextUrl = "",
                currentUrl = CurrentBook.Url,
                text = message,
                title = "An unexpected error has occured"
            };
        }
#nullable disable
        #endregion
        #endregion
    }
}
