using CommunityToolkit.Maui.Alerts;
using Gsemac.IO.Logging;
using Gsemac.Net;
using Gsemac.Net.Cloudflare.Selenium;
using Gsemac.Net.Http;
using Gsemac.Net.WebDrivers;
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
        readonly HttpClient Client;
        readonly IWebClient webClient;

        public NextChapterService(HttpClient client)
        {
            Client = client;
            using ServiceProvider serviceProvider = CreateServiceProvider();

            HttpWebRequestFactory httpWebRequestFactory = new HttpWebRequestFactory();
            WebDriverFactory factory = new WebDriverFactory();
            WebDriverChallengeHandler challengeHandler = new WebDriverChallengeHandler(httpWebRequestFactory, factory);
            IWebClientFactory webClientFactory = new WebClientFactory(httpWebRequestFactory, challengeHandler);

            webClient = webClientFactory.Create();
        }

        ~NextChapterService()
        {
            webClient.Dispose();
        }

        public static ServiceProvider CreateServiceProvider()
        {

            return new ServiceCollection()
                .AddSingleton<Gsemac.IO.Logging.ILogger, ConsoleLogger>()
                .AddSingleton<IWebClientFactory, WebClientFactory>()
                .AddSingleton<IHttpWebRequestFactory, HttpWebRequestFactory>()
                .BuildServiceProvider();

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

        public async Task<Book> CheckNextChapterBook(Book book, CancellationToken token)
        {


            string html = webClient.DownloadString(book.Url);

            Config? config = await Config.FindValidConfig(Client, book.Url, html);

            if (config is null || config.UsesWebview)
                return book;

            string nextUrl = new ConfigService(config).PrettyWrapSelector(html, ConfigService.Selector.NextUrl, SelectorType.Link);
            book.HasNextChapter = WebUtilities.IsUrl(nextUrl);
            return book;
        }

        public async Task<IEnumerable<Book>> CheckChaptersInBookList(List<Book> books, CancellationToken token)
        {
            List<Book> verifiedbooks = new();

            using ServiceProvider serviceProvider = CreateServiceProvider();
            IWebClientFactory webClientFactory = serviceProvider.GetRequiredService<IWebClientFactory>();

            using IWebClient webClient = webClientFactory.Create();

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
                    string nextUrl = new ConfigService(config).PrettyWrapSelector(html, ConfigService.Selector.NextUrl, SelectorType.Link);
                    book.HasNextChapter = WebUtilities.IsUrl(nextUrl);
                }
                catch (XPathException) { }
                verifiedbooks.Add(book);
            }
            return verifiedbooks;
        }
    }
}
