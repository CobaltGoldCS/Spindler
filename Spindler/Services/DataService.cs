using SQLite;
using Spindler.Models;

namespace Spindler.Services;

public class DataService
{
    readonly SQLiteAsyncConnection database;

    public DataService(string dbPath)
    {
        database = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create    |
                SQLiteOpenFlags.SharedCache
            );
        database.CreateTablesAsync<BookList, Book, Config>().Wait();
    }

    #region Generic functions for database

    public async Task SaveItemAsync<T>(T item)
        where T: IIndexedModel, new()
    {
        if (item.GetId() < 0)
        {
            await database.InsertAsync(item);
        }
        await database.UpdateAsync(item);
    }

    public async Task<List<T>> GetAllItemsAsync<T>() where T: IIndexedModel, new()
        => await database.Table<T>().ToListAsync();

    public async Task<int> DeleteItemAsync<T>(T item) where T: IIndexedModel, new()
        => await database.DeleteAsync(item);
    #endregion

    #region BOOKLISTS Unique Methods
    public async Task<List<BookList>> GetBookListsAsync()    
        => await database.Table<BookList>()
        .OrderByDescending((list) => list.LastAccessed)
        .ToListAsync();
    /// <summary>
    /// Delete booklist and all books associated with it 
    /// </summary>
    /// <param name="item">The booklist to delete</param>
    /// <returns>The number of books deleted</returns>
    public async Task<int> DeleteBookListAsync(BookList item)
    {
        List<Task> tasks = new();
        Task delBooklist = database.DeleteAsync(item);
        tasks.Add(delBooklist);
        foreach (Book book in await GetBooksByBooklistIdAsync(item.Id))
        {
            tasks.Add(DeleteItemAsync(book));
        }
        await Task.WhenAll(tasks);
        return tasks.Count - 1;
    }

    public async Task<BookList> GetBooklistByIdAsync(int id)
        => await database.Table<BookList>().Where((item) => item.Id == id).FirstOrDefaultAsync();
    #endregion

    #region BOOKS Unique Methods
    public async Task<List<Book>> GetBooksByBooklistIdAsync(int id)
        => await database.Table<Book>()
            .Where((item) => item.BookListId == id)
            .OrderByDescending((book) => book.LastViewed).ToListAsync();

    public async Task<Book> GetBookByIdAsync(int id)
        => await database.Table<Book>().Where((item) => item.Id == id).FirstOrDefaultAsync();
    #endregion

    #region CONFIGS Unique Methods
    public async Task<Config> GetConfigByDomainNameAsync(string domain)
        => await database.Table<Config>()
        .Where((item) => item.DomainName == domain.Replace("www.", ""))
        .FirstOrDefaultAsync();
    public async Task<Config> GetConfigByIdAsync(int id)
        => await database.Table<Config>().Where((item) => item.Id == id).FirstOrDefaultAsync();
    #endregion
}

