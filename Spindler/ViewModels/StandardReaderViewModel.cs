﻿using CommunityToolkit.Mvvm.ComponentModel;
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
        private WebService? _webservice = null;
        private WebService WebService
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
        private string title;
        [ObservableProperty]
        private string text;
        [ObservableProperty]
        private bool isLoading;


        readonly bool defaultvisible = false;
        public bool PrevButtonIsVisible
        {
            get
            {
                if (loadedData is not null)
                    return WebService.IsUrl(loadedData.prevUrl);
                else
                    return defaultvisible;
            }
        }
        public bool NextButtonIsVisible
        {
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

        [RelayCommand]
        public async void NextClick()
        {
            var nextdata = (await PreloadDataTask!)![1];
            if (!await FailIfNull(nextdata, "Invalid Url"))
            {
                await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                IsLoading = true;
                loadedData = nextdata;
                DataChanged();
            }
        }

        [RelayCommand]
        public async void PrevClick()
        {
            var prevdata = (await PreloadDataTask!)![0];
            if (!await FailIfNull(prevdata, "Invalid Url"))
            {
                await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
                IsLoading = true;
                loadedData = prevdata;
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
                Title = "Bookmark: " + loadedData!.title,
                Url = loadedData.currentUrl!,
                Position = ReadingLayout!.ScrollY / ReadingLayout.ContentSize.Height,
            });
        }
        #endregion

        #region Initialization Functions
        public StandardReaderViewModel()
        { 
            IsLoading = true;
            title = "";
            text = "";
            Shell.Current.Navigating += OnShellNavigated;
        }

        public async Task StartLoad()
        {
            if (await FailIfNull(Config, "Configuration does not exist")) return;
            LoadedData? data = await WebService.LoadUrl(CurrentBook!.Url);
            if (await FailIfNull(data, "Invalid Url")) return;
            loadedData = data!;
            // Get image url from load
            if (string.IsNullOrEmpty(CurrentBook!.ImageUrl) || CurrentBook!.ImageUrl == "no_image.jpg")
            {
                var html = await WebService.HtmlOrError(CurrentBook.Url);
                
                if (Result.IsError(html)) return;
                var doc = new HtmlDocument();
                doc.LoadHtml(html.AsOk().Value);

                CurrentBook.ImageUrl = ConfigService.PrettyWrapSelector(doc, new Models.Path(Config!.ImageUrlPath), ConfigService.SelectorType.Link) ?? "";
            }
            DataChanged();
            await DelayScroll();
        }

        private ScrollView? ReadingLayout;
        private ImageButton? BookmarkButton;

        public void AttachReferencesToUI(ScrollView readingLayout, ImageButton bookmarkButton)
        {
            ReadingLayout = readingLayout;
            BookmarkButton = bookmarkButton;
        }
        #endregion

        #region Helperfunctions

        private async void DataChanged()
        {
            if (await FailIfNull(loadedData, "This is an invalid url")) return;
            if (loadedData!.title == "afb-4893") // This means an error has occured while getting data from the WebService
            {
                Dictionary<string, object> parameters = new()
                {
                    { "errormessage", loadedData.text! },
                    { "config", loadedData.config! }
                };
                await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}", parameters);
                return;
            }
            // Database updates
            CurrentBook!.Url = loadedData.currentUrl!;
            await CurrentBook.UpdateLastViewedToNow();

            Title = loadedData.title ?? "";
            Text = loadedData.text ?? "";

            OnPropertyChanged(nameof(NextButtonIsVisible));
            OnPropertyChanged(nameof(PrevButtonIsVisible));

            PreloadDataTask = WebService.LoadData(loadedData.prevUrl, loadedData.nextUrl);
            IsLoading = false;
        }


        private async Task DelayScroll()
        {
            await Task.Run(async () =>
            {
                await Task.Delay(100);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX,
                        Math.Clamp(CurrentBook!.Position, 0d, 1d) * ReadingLayout.ContentSize.Height,
                        Config!.ExtraConfigs.GetOrDefault("autoscrollanimation", true));
                });

            });
        }

        public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
        {
            if (e.Current.Location.OriginalString == "//BookLists/BookPage/ReaderPage")
            {
                if (CurrentBook != null)
                {
                    CurrentBook.Position = ReadingLayout!.ScrollY / ReadingLayout.ContentSize.Height;
                    await App.Database.SaveItemAsync(CurrentBook);
                }
            }
            Shell.Current.Navigating -= OnShellNavigated;
        }

        #region Error Handlers
        public async Task<bool> FailIfNull(object? value, string message)
        {
            bool nullobj = value == null;
            if (nullobj)
            {
                Dictionary<string, object> parameters = new()
                {
                    { "errormessage", message },
                    { "config", Config! }
                };
                await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}", parameters);
            }
            return nullobj;
        }
        #endregion
        #endregion
    }
}