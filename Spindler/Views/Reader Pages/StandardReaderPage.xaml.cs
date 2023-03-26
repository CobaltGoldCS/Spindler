using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.ViewModels;
using Spindler.Views;

namespace Spindler;

public partial class StandardReaderPage : ContentPage, IQueryAttributable
{
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book book = (query["book"] as Book)!;
        Config? config = await Config.FindValidConfig(book.Url);

        var viewmodel = new StandardReaderViewModel()
        {
            CurrentBook = book,
        };
        if (config is not null)
        {
            viewmodel.LoadRequiredInfo(new(config));
        }
        ReadingLayout.Scrolled += viewmodel.Scrolled;
        viewmodel.AttachReferencesToUI(ReadingLayout, BookmarkItem);
        BindingContext = viewmodel;
        await viewmodel.StartLoad();
    }

    public StandardReaderPage()
    {
        InitializeComponent();
    }
}