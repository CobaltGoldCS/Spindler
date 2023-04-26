using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Reader_Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.ViewModels;

public partial class HeadlessReaderViewModel : ObservableObject, IReader
{
    #region Properties
    ReaderDataService ReaderService { get; set; }
    private ScrollView ReadingLayout { get; set; }
    Book Book { get; set; }

    [ObservableProperty]
    LoadedData loadedData =  new() { Title = "Loading" };

    [ObservableProperty]
    bool prevVisible = false;

    [ObservableProperty]
    bool nextVisible = false;

    [ObservableProperty]
    bool isLoading = true;

    readonly CancellationTokenSource nextChapterTaskToken = new();

    #endregion


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public HeadlessReaderViewModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Shell.Current.Navigating += OnShellNavigated;
    }

    public void SetProperties(Book book, ReaderDataService dataService, ScrollView readerLayout)
    {
        Book = book;
        ReaderService = dataService;
        ReadingLayout = readerLayout;
    }

    #region commands

    [RelayCommand]
    private async void Bookmark()
    {
        await App.Database.SaveItemAsync<Book>(new()
        {
            BookListId = Book!.BookListId,
            Title = "Bookmark: " + LoadedData.Title,
            Url = LoadedData.currentUrl!,
            Position = ReadingLayout!.ScrollY,
        });
    }

    [RelayCommand]
    private async void PrevButton()
    {
        IsLoading = true;
        Book!.Url = LoadedData.prevUrl!;
        PrevVisible = false;
        NextVisible = false;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        await GetContentAsLoadedData(Book.Url);
        await UpdateUiUsingLoadedData();
    }

    [RelayCommand]
    private async void NextButton()
    {
        IsLoading = true;
        PrevVisible = false;
        NextVisible = false;
        Book!.Url = LoadedData.nextUrl!;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        await GetContentAsLoadedData(Book.Url);
        await UpdateUiUsingLoadedData();
    }

    #endregion

    public async Task Run()
    {
        NextChapterService chapterService = new();
        await Book.UpdateViewTimeAndSave();
        IDispatcher? dispatcher = Dispatcher.GetForCurrentThread();
        await Task.Run(async () =>
        {
            IEnumerable<Book> updateQueue = await chapterService.CheckChaptersInBookList(Book, nextChapterTaskToken.Token);
            await dispatcher!.DispatchAsync(async () => await App.Database.SaveItemsAsync(updateQueue));
        });

        await GetContentAsLoadedData(Book.Url);
        await UpdateUiUsingLoadedData();
    }


    /// <summary>
    /// Find and extract content into <see cref="LoadedData"/>
    /// </summary>
    private async Task GetContentAsLoadedData(string url)
    {
        IResult<LoadedData> resultContent = await ReaderService!.LoadUrl(url);

        if (resultContent is Invalid<LoadedData> error)
        {
            await SafeAssert(false, error.Value.getMessage());
            return;
        }

        LoadedData = (resultContent as Ok<LoadedData>)!.Value;

        if (!await SafeAssert(!string.IsNullOrEmpty(LoadedData.Text), "Content path unable to match html"))
            return;

    }

    /// <summary>
    /// Updates the User Interface using <see cref="LoadedData"/>
    /// </summary>
    private async Task UpdateUiUsingLoadedData()
    {
        NextVisible = WebUtilities.IsUrl(LoadedData.nextUrl);
        PrevVisible = WebUtilities.IsUrl(LoadedData.prevUrl);

        // Turn relative urls into absolutes
        var baseUri = new Uri(LoadedData.currentUrl!);
        LoadedData.prevUrl = new Uri(baseUri, LoadedData.prevUrl).ToString();
        LoadedData.nextUrl = new Uri(baseUri, LoadedData.nextUrl).ToString();


        await ScrollToLastReadPositionIfApplicable();
        IsLoading = false;
    }

    /// <summary>
    /// If the book has a previous position, scroll to it
    /// </summary>
    private async Task ScrollToLastReadPositionIfApplicable()
    {
        if (Book.Position <= 0)
        {
            return;
        }

        await Task.Run(async () =>
        {
            while (ReadingLayout.ContentSize.Height < 1000)
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX,
                    Book.Position,
                    ReaderService!.Config.HasAutoscrollAnimation);
                Book.Position = 0;
            });
        });
    }

    public async void OnShellNavigated(object? _,
                           ShellNavigatingEventArgs e)
    {
        if (e.Target.Location.OriginalString == "..")
        {
            Book.Position = ReadingLayout.ScrollY;
            Book.HasNextChapter = NextVisible;
            await Book.UpdateViewTimeAndSave();
        }
        // This will safely cancel the nextChapter task
        nextChapterTaskToken.Cancel();

        Shell.Current.Navigating -= OnShellNavigated;
    }

    /// <summary>
    /// Assert that <paramref name="condition"/> is true or fail with <see cref="ErrorPage"/>
    /// </summary>
    /// <param name="condition">The condition to assert if true</param>
    /// <param name="message">A message for the <see cref="ErrorPage"/> to display</param>
    /// <returns>The result of the condition</returns>
    public async Task<bool> SafeAssert(bool condition, string message)
    {
        if (condition)
            return true;

        Dictionary<string, object> parameters = new()
        {
            { "errormessage", message },
            { "config", ReaderService.Config }
        };

        await Shell.Current.GoToAsync($"/{nameof(ErrorPage)}", parameters);
        return false;
    }


}
