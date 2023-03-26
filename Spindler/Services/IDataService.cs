using Spindler.Models;

namespace Spindler.Services;

public interface IDataService
{
    #region Generic Methods
    Task SaveItemAsync<T>(T item)
        where T : IIndexedModel, new();

    Task SaveItemsAsync<T>(IEnumerable<T> items)
        where T : IIndexedModel, new();

    Task<List<T>> GetAllItemsAsync<T>() where T : IIndexedModel, new();
    
    Task<int> DeleteItemAsync<T>(T item) where T : IIndexedModel, new();
    
    Task<T> GetItemByIdAsync<T>(int id) where T : IIndexedModel, new();

    #endregion

    #region Item-Specific methods
    Task<List<BookList>> GetBookListsAsync();

    Task<int> DeleteBookListAsync(BookList item);

    Task<List<Book>> GetBooksByBooklistIdAsync(int id);

    Task<Config> GetConfigByDomainNameAsync(string domain);

    #endregion
}
