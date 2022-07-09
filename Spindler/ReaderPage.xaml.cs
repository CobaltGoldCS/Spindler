using CommunityToolkit.Maui.Alerts;
using Spindler.Models;
using Spindler.Services;

namespace Spindler;

[QueryProperty(nameof(BookId), "id")]
public partial class ReaderPage : ContentPage
{
    #region Class Attributes
    private readonly WebService webService;

    public Book currentBook;
    public Config config { get; set; }
	private LoadedData loadedData { get; set; }

    /// <summary>
    /// Task that should hold an array of length 2 containing (previous chapter, next chapter) in that order
    /// </summary>
	private Task<LoadedData[]> PreloadDataTask;
    #endregion

    #region QueryProperty Handler
    public string BookId
    {
		set
        {
			LoadBook(Convert.ToInt32(value));
        }
	}

    private async void LoadBook(int id)
    {
        currentBook = await App.Database.GetBookByIdAsync(id);
        Config config = await App.Database.GetConfigByDomainNameAsync(new UriBuilder(currentBook.Url).Host);

        if (FailIfNull(config, "Configuration does not exist")) return;
        LoadedData data = await webService.PreloadUrl(currentBook.Url, config);
        if (FailIfNull(data, "Invalid Url")) return;
        loadedData = data;
        DataChanged();
    }
    #endregion

    public ReaderPage()
    {
        webService = new WebService();
        InitializeComponent();
    }

    #region Click Handlers
    private async void Previous_Clicked(object sender, EventArgs e)
    {
        var prevdata = (await PreloadDataTask)[0];
        if (!FailIfNull(prevdata, "Invalid Url"))
        {
            loadedData = prevdata;
            DataChanged();
        }
    }

    private async void Next_Clicked(object sender, EventArgs e)
    {
        var nextdata = (await PreloadDataTask)[1];
        if (!FailIfNull(nextdata, "Invalid Url"))
        {
            loadedData = nextdata;
            DataChanged();
        }
    }
    #endregion

    #region Helperfunctions
    private async void DataChanged()
    {
        if (FailIfNull(loadedData, "This is an invalid url")) return;
        BindingContext = loadedData;
        // Database updates
        currentBook.Url = loadedData.currentUrl;
        currentBook.LastViewed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(currentBook);

        PreloadDataTask = webService.PreloadData(loadedData.prevUrl, loadedData.nextUrl, config);
        UpdateUI();
    }


    private async void UpdateUI()
    {
        PrevButton.IsVisible = WebService.IsUrl(loadedData.prevUrl);
        NextButton.IsVisible = WebService.IsUrl(loadedData.nextUrl);
        ContentView.Text = loadedData.text;
        TitleView.Text = loadedData.title;
        Title = loadedData.title;
        ProperlyRenderScrollWorkaround();
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
    }

    /// <summary>
    /// Workaround to get content to properly render in scrollview update
    /// </summary>
    private async void ProperlyRenderScrollWorkaround() => await Task.Run(() =>
        {
            var content = ReadingLayout.Content;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ReadingLayout.Content = null;
                ReadingLayout.Content = content;
            });
        });

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
            loadedData = MakeFailMessage(message);
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
            currentUrl = currentBook.Url,
            text = message,
            title = "An unexpected error has occured"
        };
    }
#nullable disable
    #endregion
}