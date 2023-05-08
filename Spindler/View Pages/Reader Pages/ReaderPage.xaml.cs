using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;

namespace Spindler;

public partial class ReaderPage : ContentPage, IQueryAttributable
{
    public enum ReaderType
    {
        Standard,
        Headless
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book book = (query["book"] as Book)!;
        
        ReaderType type = (ReaderType)query["type"];
        if (!query.TryGetValue("config", out object? config))
        {
            config = await Config.FindValidConfig(book.Url);
        }

        var viewmodel = new ReaderViewModel();
        
        // Attach required information to View Model
        if (config is not null)
        {

            viewmodel.SetRequiredInfo(new((Config)config, type switch
            {
                ReaderType.Headless => new HeadlessWebService(HeadlessBrowser),
                ReaderType.Standard => new StandardWebService(),
                _ => throw new NotImplementedException("This service has not been implemented")
            }));
        }
        viewmodel.SetReferencesToUI(ReadingLayout, BookmarkItem);
        viewmodel.SetCurrentBook(book);
        ReadingLayout.Scrolled += viewmodel.Scrolled;

        BindingContext = viewmodel;
        await viewmodel.StartLoad();
    }

    public ReaderPage()
    {
        InitializeComponent();
    }
}