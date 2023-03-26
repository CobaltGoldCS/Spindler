namespace Spindler.Views;

using Spindler.Models;
using Spindler.ViewModels;
using System.Collections.Generic;

public partial class BookListPage : ContentPage, IQueryAttributable
{
    BookListViewModel viewmodel;
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        BookList? bookList = query["booklist"]! as BookList;
        await bookList!.UpdateAccessTimeToNow();

        viewmodel.SetBookList(bookList);
        viewmodel.AddUiReferences(AddToolBarItem);
        BindingContext = viewmodel;
        await viewmodel.Load();
    }

    public BookListPage(BookListViewModel viewmodel)
    {
        InitializeComponent();
        this.viewmodel = viewmodel;
        BooksList.Unfocus();
    }
}