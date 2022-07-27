using Spindler.Services;
using Spindler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Spindler.ViewModels
{
    public class ReaderViewModel : INotifyPropertyChanged
    {
        #region Class Attributes
        private readonly WebService webService;

        public Book CurrentBook;
        public Config Config { get; set; }

        /// <summary>
        /// Task that should hold an array of length 2 containing (previous chapter, next chapter) in that order
        /// </summary>
        private Task<LoadedData[]> PreloadDataTask;

        #region Bindable Properties
        private LoadedData loadedData;
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

        private string title;
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
        private string text;
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
        #endregion
        #endregion

        #region Click Handlers
        public ICommand NextClickHandler { get; private set; }
        public ICommand PrevClickHandler { get; private set; }
        #endregion

#nullable enable
        public ReaderViewModel()
        {
            webService = new WebService();
            #region Click Handler Implementations
            PrevClickHandler = new Command(async () =>
            {
                var prevdata = (await PreloadDataTask)[0];
                if (!FailIfNull(prevdata, "Invalid Url"))
                {
                    loadedData = prevdata;
                    DataChanged();
                }
            });
            NextClickHandler = new Command(async () =>
            {
                var nextdata = (await PreloadDataTask)[1];
                if (!FailIfNull(nextdata, "Invalid Url"))
                {
                    loadedData = nextdata;
                    DataChanged();
                }
            });
            #endregion
        }

        public async Task StartLoad()
        {
            if (FailIfNull(Config, "Configuration does not exist")) return;
            LoadedData data = await webService.PreloadUrl(CurrentBook.Url, Config);
            if (FailIfNull(data, "Invalid Url")) return;
            LoadedData = data;
            DataChanged();
            await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, CurrentBook.Position, true);
        }
#nullable disable

        private ScrollView ReadingLayout;
        public void AttachReferencesToUI(ScrollView readingLayout)
        {
            ReadingLayout = readingLayout;
        }

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

            PreloadDataTask = webService.PreloadData(LoadedData.prevUrl, LoadedData.nextUrl, Config);
            ScrollWorkaround();
            await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        }

        private void ScrollWorkaround()
        {
            var content = ReadingLayout.Content;
            ReadingLayout.Content = null;
            ReadingLayout.Content = content;
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
                DataChanged();
            }
            return nullobj;
        }

        private LoadedData MakeFailMessage(string message)
        {
            return new LoadedData
            {
                prevUrl = "",
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
