using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;

namespace Spindler;

public partial class StandardReaderPage : ContentPage, IQueryAttributable
{
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book book = (query["book"] as Book)!;
        Config? config = await Config.FindValidConfig(book.Url);

        var viewmodel = new StandardReaderViewModel();
        
        // Attach required information to View Model
        if (config is not null)
        {
            viewmodel.SetRequiredInfo(new(config, new StandardWebService()));
        }
        viewmodel.SetReferencesToUI(ReadingLayout, BookmarkItem);
        viewmodel.SetCurrentBook(book);
        ReadingLayout.Scrolled += viewmodel.Scrolled;

        BindingContext = viewmodel;
        await viewmodel.StartLoad();
    }

    public StandardReaderPage()
    {
        InitializeComponent();
    }
}