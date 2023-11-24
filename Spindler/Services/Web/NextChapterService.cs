using CommunityToolkit.Maui.Alerts;
using Gsemac.Net;
using Gsemac.Net.Http;
using Microsoft.Extensions.Logging;
using Spindler.Models;
using Spindler.Utilities;
using System.Net;
using System.Xml.XPath;
using Path = Spindler.Models.Path;

namespace Spindler.Services.Web
{
    public class NextChapterService
    {
        HttpClient Client;
        public NextChapterService(HttpClient client)
        {
            Client = client;
        }

        private class WebClient : WebClientBase
        {
            public WebClient(IHttpWebRequestFactory webRequestFactory, WebRequestHandler webRequestHandler)
                : base(webRequestFactory, webRequestHandler)
            {
            }
        }

        /// <summary>
        /// Checks all the books in a booklist with <paramref name="booklistId"/> for their next chapter and updates the database
        /// </summary>
        /// <param name="booklistId">The id of the target booklist</param>
        /// <param name="token">A token to cancel the operation if needed</param>
        /// <returns></returns>
        public async Task<IEnumerable<Book>> CheckChaptersInBookList(int booklistId, CancellationToken token)
        {
            List<Book> books = await App.Database.GetBooksByBooklistIdAsync(booklistId);
            List<Book> filteredBooks = books.FindAll((book) => !book.HasNextChapter);
            return await CheckChaptersInBookList(filteredBooks, token);
        }

        public async Task<IEnumerable<Book>> CheckChaptersInBookList(Book book, CancellationToken token)
        {
            List<Book> books = await App.Database.GetBooksByBooklistIdAsync(book.BookListId);
            List<Book> filteredBooks = books.FindAll((filterBook) => !filterBook.HasNextChapter && filterBook.Id != book.Id);
            return await CheckChaptersInBookList(filteredBooks, token);
        }

        public async Task<IEnumerable<Book>> CheckChaptersInBookList(List<Book> books, CancellationToken token)
        {
            List<Book> verifiedbooks = new();

            using WebRequestHandler handler = new();
            using IWebClient webClient = WebClientFactory.Default.Create();

            foreach (Book book in books)
            {
                if (token.IsCancellationRequested) return verifiedbooks;

                string html;
                try
                {
                    html = webClient.DownloadString(book.Url);
                }
                catch (System.Net.WebException) { continue; }
                Config? config = await Config.FindValidConfig(Client, book.Url, html);
                if (config is null || config.UsesWebview)
                    continue;
                try
                {
                    string nextUrl = ConfigService.PrettyWrapSelector(html, new(config.NextUrlPath), ConfigService.SelectorType.Link);
                    book.HasNextChapter = WebUtilities.IsUrl(nextUrl);
                }
                catch (XPathException) { }
                verifiedbooks.Add(book);
            }
            return verifiedbooks;
        }
    }
}
