namespace Spindler.Views;

using Spindler.Models;
using Spindler.ViewModels;
using System.Collections.Generic;

public partial class BookListPage : ContentPage, IQueryAttributable
{
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        BookList? bookList = query["booklist"]! as BookList;
        await bookList!.UpdateAccessTimeToNow();

        var binding = new BookListViewModel(bookList);
        binding.AddUiReferences(AddToolBarItem);
        BindingContext = binding;
        await binding.Load();
    }

    public BookListPage()
    {
        InitializeComponent();
        BooksList.Unfocus();
    }
}