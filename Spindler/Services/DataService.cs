using Spindler.Models;
using SQLite;

namespace Spindler.Services;

public class DataService : IDataService
{
    private readonly SQLiteAsyncConnection database;

    public DataService(string dbPath)
    {
        database = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache
            );
        database.CreateTablesAsync(createFlags: CreateFlags.None, typeof(Book), typeof(BookList), typeof(Config), typeof(GeneralizedConfig)).Wait();
    }

    // NOTE: This is used for dependency injection
    public DataService()
    {
        database = new SQLiteAsyncConnection(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spindler.db"), 
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache
            );
        database.CreateTablesAsync(createFlags: CreateFlags.None, typeof(Book), typeof(BookList), typeof(Config), typeof(GeneralizedConfig)).Wait();
    }

    #region Generic functions for database

    public async Task SaveItemAsync<T>(T item)
        where T : IIndexedModel, new()
    {
        if (item.GetId() < 0)
        {
            await database.InsertAsync(item);
        }
        await database.UpdateAsync(item);
    }

    public async Task SaveItemsAsync<T>(IEnumerable<T> items)
        where T : IIndexedModel, new()
    {
        var itemsToInsert = items.Where((T item) => item.GetId() < 0);
        var itemsToUpdate = items.Where((T item) => item.GetId() >= 0);
        await database.UpdateAllAsync(itemsToUpdate);
        await database.InsertAllAsync(itemsToInsert);
    }

    public async Task<List<T>> GetAllItemsAsync<T>() where T : IIndexedModel, new()
        => await database.Table<T>().ToListAsync();

    public async Task<int> DeleteItemAsync<T>(T item) where T : IIndexedModel, new()
        => await database.DeleteAsync(item);

    public async Task<T> GetItemByIdAsync<T>(int id) where T : IIndexedModel, new()
        => await database.FindAsync<T>(id);
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
    #endregion

    #region BOOKS Unique Methods
    public async Task<List<Book>> GetBooksByBooklistIdAsync(int id)
        => await database.Table<Book>()
            .Where((item) => item.BookListId == id)
            .OrderByDescending((book) => book.LastViewed).ToListAsync();
    #endregion

    #region CONFIGS Unique Methods
    public async Task<Config> GetConfigByDomainNameAsync(string domain)
        => await database.Table<Config>()
        .Where((item) => item.DomainName == domain.Replace("www.", ""))
        .FirstOrDefaultAsync();
    #endregion
}

