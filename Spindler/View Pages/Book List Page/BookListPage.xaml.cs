namespace Spindler.Views;

using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;
using System.Collections.Generic;

public partial class BookListPage : ContentPage, IQueryAttributable
{
    IDataService Database;
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        BookList? bookList = query["booklist"]! as BookList;

        var ViewModel = (BookListViewModel)BindingContext;

        await bookList!.UpdateAccessTimeToNow(Database).ConfigureAwait(false);
        ViewModel.SetBookListAndProperties(bookList);
        // Populate Lists
        await MainThread.InvokeOnMainThreadAsync(ViewModel.EndOfListReached);
    }
    public BookListPage(IDataService database, BookListViewModel viewmodel)
    {
        InitializeComponent();
        Database = database;

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