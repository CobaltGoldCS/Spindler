using CommunityToolkit.Mvvm.Input;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.ViewModels;
using Spindler.Views.Reader_Pages;
using System.ComponentModel;

namespace Spindler.Views;

public partial class HeadlessReaderPage : ContentPage, INotifyPropertyChanged, IQueryAttributable
{
    public HeadlessReaderPage()
    {
        InitializeComponent();
    }


    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book book = (query["book"] as Book)!;

        ReaderDataService readerService = new ReaderDataService(new Config(), new HeadlessWebService(HeadlessBrowser));


        if (!await SafeAssert(await readerService.setConfigFromUrl(book.Url), "Could not get configuration information from website"))
            return;

        ((HeadlessReaderViewModel)BindingContext).SetProperties(book, readerService, ReadingLayout);
        await ((HeadlessReaderViewModel)BindingContext).Run();
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
            { "config", new Config { Id = -1 } }
        };

        await Shell.Current.GoToAsync($"/{nameof(ErrorPage)}", parameters);
        return false;
    }
}