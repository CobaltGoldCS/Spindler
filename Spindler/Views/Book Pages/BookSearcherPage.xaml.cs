using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HtmlAgilityPack;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using System.Text.RegularExpressions;

namespace Spindler.Views.Book_Pages;

[QueryProperty(nameof(BooklistId), "bookListid")]
public partial class BookSearcherPage : ContentPage
{
    public enum State
    {
        BookFound,
        BookNotFound,
        BookSaved
    }

    public int BooklistId { get; set; } = new();
    public Config? Config { get; set; } = null;

    private string source = "example.com";
    public string Source
    {
        get => source;
        set
        {
            source = value;
            OnPropertyChanged();
        }
    }


    public BookSearcherPage()
	{
		InitializeComponent();
        SwitchUiBasedOnState(State.BookNotFound);
    }

    private async Task CheckCompatible(string url = "")
    {
        var html = await SearchBrowser.EvaluateJavaScriptAsync(
            "'<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>';");
        html = Regex.Unescape(html ?? "");

        Config = await WebService.FindValidConfig(!string.IsNullOrEmpty(url) ? url : GetUrlOfBrowser(), html);
        if (Config is null) return;

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        string? content = ConfigService.PrettyWrapSelector(doc, new Models.Path(Config.ContentPath), ConfigService.SelectorType.Text);
        SwitchUiBasedOnState(!string.IsNullOrEmpty(content) ? State.BookFound : State.BookNotFound);
        await SearchProgress.ProgressTo(1, 500, Easing.BounceOut);
    }

    private void SwitchUiBasedOnState(State state)
    {
        CheckButton.IsVisible = false;
        FoundButton.IsVisible = false;
        switch (state)
        {
            case State.BookNotFound:
                CheckButton.IsVisible = true;
                break;
            case State.BookFound:
                FoundButton.IsVisible = true;
                FoundButton.IsEnabled = true;
                break;
            case State.BookSaved:
                FoundButton.IsVisible = true;
                FoundButton.IsEnabled = false;
                break;
        }
    }

    private string GetUrlOfBrowser() => ((UrlWebViewSource)SearchBrowser.Source).Url;

    private async void PageLoaded(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success) return;
        // Redirect
        if (GetUrlOfBrowser() != e.Url)
        {
            SearchBrowser.Source = e.Url;
        }
        await SearchProgress.ProgressTo(.5, 500, Easing.BounceOut);
        await CheckCompatible(e.Url != "about:blank" ? e.Url : "");
    }
    private async void PageLoading(object? sender, WebNavigatingEventArgs e)
    {
        Source = e.Url;
        SwitchUiBasedOnState(State.BookNotFound);
        await SearchProgress.ProgressTo(0, 0, Easing.BounceOut);
    }

    private async void CheckIfBookIsPossible(object sender, EventArgs e)
    {
        await CheckCompatible();
    }

    private async void FoundButton_Clicked(object sender, EventArgs e)
    {
        var html = await SearchBrowser.EvaluateJavaScriptAsync(
            "'<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>';");
        html = Regex.Unescape(html);

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        string? title = ConfigService.PrettyWrapSelector(doc, new Models.Path(Config!.ContentPath), ConfigService.SelectorType.Text);
        await App.Database.SaveItemAsync(
            new Book
            {
                Id = -1,
                BookListId = BooklistId,
                LastViewed = DateTime.UtcNow,
                Title = title ?? "Could not find title for book",
                Url = GetUrlOfBrowser()
            }
        );
        SwitchUiBasedOnState(State.BookSaved);
    }

    private async void UseConfig_Clicked(object sender, EventArgs e)
    {
        PickerPopup popup = new("Choose a config to search", await App.Database.GetAllItemsAsync<Config>());
        var result = await this.ShowPopupAsync(popup);


        if (result is not Config config || !Uri.TryCreate("https://" + config.DomainName, new UriCreationOptions(), out Uri? url)) return;
        SearchBrowser.Source = url;
        source = url?.OriginalString ?? "";
    }

    [RelayCommand]
    public void Return()
    {
        if (!Source.StartsWith("http"))
            Source = "https://" + Source;
        SearchBrowser.Source = source;
    }

}