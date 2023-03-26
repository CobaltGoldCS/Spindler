using CommunityToolkit.Maui.Alerts;
using HtmlAgilityPack;
using Knyaz.Optimus;
using Knyaz.Optimus.Scripting.Jurassic;
using Spindler.CustomControls;
using Spindler.Models;

namespace Spindler.Services
{
    public class NextChapterService
    {
        public NextChapterService() 
        { 
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

        public async Task<IEnumerable<Book>> CheckChaptersInBookList(List<Book> books, CancellationToken token) {
            List<Book> verifiedbooks = new();

            var engine = EngineBuilder.New().Build();

            foreach (Book book in books) {
                if (token.IsCancellationRequested) return verifiedbooks;

                var document = await engine.OpenUrl(book.Url);
                string html = document.Document.InnerHTML;
                Config? config = await Config.FindValidConfig(book.Url, html);
                if (config is null || (bool)config.ExtraConfigs.GetValueOrDefault("webview", false)) 
                    continue;

                var nextUrl = ConfigService.PrettyWrapSelector(html, new(config.NextUrlPath), ConfigService.SelectorType.Link);

                book.HasNextChapter = nextUrl.Length > 0;
                verifiedbooks.Add(book);
            }
            return verifiedbooks;
        }
    }
}
