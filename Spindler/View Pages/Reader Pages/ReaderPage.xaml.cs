using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.Models;
using Spindler.Services;
using Spindler.Services.Web;
using Spindler.ViewModels;

namespace Spindler;

public partial class ReaderPage : ContentPage, IQueryAttributable, IRecipient<ChangeScrollMessage>
{
    HttpClient Client { get; set; }
    IDataService DataService { get; set; }

    public enum ReaderType
    {
        Standard,
        Headless
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book book = (query["book"] as Book)!;

        ReaderType type = (ReaderType)query["type"];
        if (!query.TryGetValue("config", out object? configObject))
        {
            configObject = await Config.FindValidConfig(DataService, Client, book.Url);
        }



        if (configObject is null)
        {
            await Toast.Make("Error: Couldn't find configuration").Show();
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.GoToAsync(".."));
            });
            return;
        }

        var ViewModel = new ReaderViewModelBuilder(DataService, Client, NextChapterBrowser)
            .SetRequiredInfo(new((Config)configObject, type switch
            {
                ReaderType.Headless => HeadlessBrowser,
                ReaderType.Standard => new StandardWebService(Client),
                _ => throw new NotImplementedException("This service has not been implemented")
            }))
            .SetCurrentBook(book)
            .Build();

        BindingContext = ViewModel;

        await ViewModel.StartLoad();
    }

    /// <summary>
    /// In charge of scrolling to positions. NOTE: Negative values scroll to bottom
    /// </summary>
    /// <param name="message">A message containing position (double) and isAnimated (bool)</param>
    async void IRecipient<ChangeScrollMessage>.Receive(ChangeScrollMessage message)
    {
        ScrollChangedArgs arguments = message.Value;
        if (arguments.Position < 0)
        {
            await ReadingLayout.ScrollToAsync(ReaderView, ScrollToPosition.End, arguments.IsAnimated);
        }
        else
        {
            await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, arguments.Position, arguments.IsAnimated);
        }
    }



    public ReaderPage(HttpClient client, IDataService dataService)
    {
        InitializeComponent();
        Client = client;
        DataService = dataService;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
}