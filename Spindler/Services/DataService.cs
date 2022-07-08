using SQLite;
using Spindler.Models;

namespace Spindler.Services;

public class DataService
{
    #region Generic functions + Saving Items to database
    readonly SQLiteAsyncConnection database;

    public DataService(string dbPath)
    {
        database = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache
            );
        database.CreateTablesAsync<BookList, Book, Config>().Wait();
    }

    public async Task SaveItemAsync(IIndexedModel item)
    {
        if (item.GetId() < 0)
        {
            await database.InsertAsync(item);
        }
        await database.UpdateAsync(item);
    }

    #endregion

    #region BOOKLISTS RUD
    public async Task<List<BookList>> GetBookListsAsync()    
        => await database.Table<BookList>()
        .OrderByDescending((list) => list.LastAccessed)
        .ToListAsync();
    public async Task<BookList> GetBookListByIdAsync(int id) 
        => await database.Table<BookList>()
        .Where((item) => item.Id == id)
        .FirstOrDefaultAsync();
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
            tasks.Add(DeleteBookAsync(book));
        }
        await Task.WhenAll(tasks);
        return tasks.Count - 1;
    }
    #endregion

    #region BOOKS RUD
    public async Task<int> DeleteBookAsync(Book item) => await database.DeleteAsync(item);
    public async Task<Book> GetBookByIdAsync(int id) => await database.Table<Book>().Where((item) => item.Id == id).FirstOrDefaultAsync();
    public async Task<List<Book>> GetBooksByBooklistIdAsync(int id)
        => await database.Table<Book>()
            .Where((item) => item.BookListId == id)
            .OrderByDescending((book) => book.LastViewed).ToListAsync();
    #endregion

    #region CONFIGS RUD
    public async Task<int> DeleteConfigAsync(Config item) => await database.DeleteAsync(item);
    public async Task<Config> GetConfigByIdAsync(int id)
        => await database.Table<Config>()
        .Where((item) => item.GetId() == id)
        .FirstOrDefaultAsync();

    public async Task<Config> GetConfigByDomainNameAsync(string domain)
        => await database.Table<Config>()
        .Where((item) => item.DomainName == domain)
        .FirstOrDefaultAsync();
    public Task<List<Config>> GetConfigsAsync() => database.Table<Config>().ToListAsync();
    #endregion
}

