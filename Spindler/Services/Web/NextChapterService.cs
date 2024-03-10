using Spindler.Models;
using Spindler.Utilities;

namespace Spindler.Services.Web
{
    public class NextChapterService(HttpClient client, IWebService browser, IDataService database)
    {
        private readonly HttpClient _Client = client;
        private readonly IWebService _WebService = browser;
        private readonly IDataService _Database = database;

        public async Task SaveBooks(List<Book> books, CancellationToken token)
        {
            foreach (Book book in books)
            {
                Book updated = await CheckNextChapterBook(book, token);
                await _Database.SaveItemAsync(updated);
            }
        }

        public async Task<Book> CheckNextChapterBook(Book book, CancellationToken token)
        {

            if (book.HasNextChapter)
            {
                return book;
            }

            Result<string> result = await _WebService.GetHtmlFromUrl(book.Url, token);

            if (result is Result<string>.Err)
            {
                return book;
            }

            string html = (result as Result<string>.Ok)!.Value;
            Config? config = await Config.FindValidConfig(_Database, _Client, book.Url, html);

            if (config is null || config.UsesWebview)
                return book;

            string nextUrl = new ConfigService(config).PrettyWrapSelector(html, ConfigService.Selector.NextUrl, SelectorType.Link);
            book.HasNextChapter = WebUtilities.IsUrl(nextUrl);
            return book;
        }
    }
}
