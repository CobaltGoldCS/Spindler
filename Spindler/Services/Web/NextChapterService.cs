using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Utilities;

namespace Spindler.Services.Web
{
    public class NextChapterService(HttpClient client, WebScraperBrowser browser)
    {
        readonly HttpClient Client = client;
        readonly WebScraperBrowser Browser = browser;

        public async Task<Book> CheckNextChapterBook(Book book, CancellationToken token)
        {

            if (book.HasNextChapter)
            {
                return book;
            }

            HeadlessWebService service = new(Browser);
            Result<string> result = await service.GetHtmlFromUrl(book.Url, token);

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
