using CommunityToolkit.Maui.Alerts;
using Knyaz.Optimus;
using Spindler.Models;
using Spindler.Utilities;
using System.Xml.XPath;
using Path = Spindler.Models.Path;

namespace Spindler.Services
{
    public class NextChapterService
    {
        HttpClient Client;
        public NextChapterService(HttpClient client)
        {
            Client = client;
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

            var engine = EngineBuilder.New().Build();

            foreach (Book book in books)
            {
                if (token.IsCancellationRequested) return verifiedbooks;

                Knyaz.Optimus.Page document;
                try
                {
                    document = await engine.OpenUrl(book.Url);
                }
                catch (System.Net.WebException)
                {
                    await Toast.Make($"Could not search for {book.Url} (Next Chapter Detector)").Show(token);
                    continue;
                }
                string html = document.Document.InnerHTML;
                Config? config = await Config.FindValidConfig(Client, book.Url, html);
                if (config is null || config.UsesWebview)
                    continue;

                string nextUrl;
                try
                {
                    nextUrl = config.NextUrlPath.AsPath().Select(html, SelectorType.Link);
                    book.HasNextChapter = WebUtilities.IsUrl(nextUrl);
                } catch (XPathException) { }
                verifiedbooks.Add(book);
            }
            return verifiedbooks;
        }
    }
}
