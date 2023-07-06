namespace Spindler.Views;

using Spindler.Models;
using Spindler.ViewModels;
using System.Collections.Generic;

public partial class BookListPage : ContentPage, IQueryAttributable
{
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        BookList? bookList = query["booklist"]! as BookList;
        await bookList!.UpdateAccessTimeToNow().ConfigureAwait(false);
        ((BookListViewModel)BindingContext).AddUiReferences(AddToolBarItem);
        ((BookListViewModel)BindingContext).SetBookListAndProperties(bookList);
    }
    public BookListPage(BookListViewModel viewmodel)
    {
        InitializeComponent();

        string layoutType = Preferences.Default.Get("book list layout", "List");

        DataTemplate bookLayout = (DataTemplate)Resources[layoutType + "Layout"];
        ItemsLayout itemsLayout = (ItemsLayout)Resources[layoutType + "ItemsLayout"];
        
        BooksList.ItemTemplate = bookLayout;
        BooksList.ItemsLayout = itemsLayout;

        BooksList.ItemSizingStrategy = layoutType switch
        {
            "List" => ItemSizingStrategy.MeasureFirstItem,
            _ => ItemSizingStrategy.MeasureAllItems
        };

        BindingContext = viewmodel;
        BooksList.Unfocus();
    }
}