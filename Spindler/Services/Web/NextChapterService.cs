using CloudKit;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Utilities;

namespace Spindler.Services.Web
{
    public class NextChapterService
    {
        readonly HttpClient Client;
        readonly HeadlessWebService Service;
        readonly IDataService Database;

        public NextChapterService(HttpClient client, WebScraperBrowser browser, IDataService database)
        {
            Client = client;
            Service = new(browser);
            Database = database;
        }

        public async Task SaveBooks(List<Book> books, CancellationToken token)
        {
            foreach (Book book in books)
            {
                Book updated = await CheckNextChapterBook(book, token);
                await Database.SaveItemAsync(updated);
            }
        }

        public async Task<Book> CheckNextChapterBook(Book book, CancellationToken token)
        {

            if (book.HasNextChapter)
            {
                return book;
            }

            Result<string> result = await Service.GetHtmlFromUrl(book.Url, token);

            if (result is Result<string>.Err)
            {
                return book;
            }

            string html = (result as Result<string>.Ok)!.Value;
            Config? config = await Config.FindValidConfig(Client, book.Url, html);

            if (config is null || config.UsesWebview)
                return book;

            string nextUrl = new ConfigService(config).PrettyWrapSelector(html, ConfigService.Selector.NextUrl, SelectorType.Link);
            book.HasNextChapter = WebUtilities.IsUrl(nextUrl);
            return book;
        }
    }
}
