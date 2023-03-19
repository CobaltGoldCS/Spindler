using CommunityToolkit.Maui.Alerts;
using HtmlAgilityPack;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Views.Book_Pages;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Spindler.Services {
    public class NextChapterService
    {
        public NextChapterService() { }

        public async Task Run(CancellationToken token) {
            List<BookList> bookLists = await App.Database.GetBookListsAsync();
            Task[] processes = new Task[bookLists.Count];

            int i = 0;
            foreach (BookList bookList in bookLists) {
                processes[i] = CheckChaptersInBookList(bookList.Id, token);
                i++;
            }
            try
            {
                await Task.WhenAll(processes);
            } catch (TaskCanceledException)
            {
                await Toast.Make("Stopped Looking for New Chapters", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
            }
        }

        /// <summary>
        /// Checks all the books in a booklist with <paramref name="booklistId"/> for their next chapter and updates the database
        /// </summary>
        /// <param name="booklistId">The id of the target booklist</param>
        /// <param name="browser">A webscraper browser instance</param>
        /// <param name="token">A token to cancel the operation if needed</param>
        /// <returns></returns>
        public async Task<IEnumerable<Book>> CheckChaptersInBookList(int booklistId, CancellationToken token, WebScraperBrowser? browser = null)
        {
            browser ??= new WebScraperBrowser();
            List<Book> books = await App.Database.GetBooksByBooklistIdAsync(booklistId);
            List<Book> filteredBooks = books.FindAll((book) => !book.HasNextChapter);
            return await CheckChaptersInBookList(filteredBooks, token, browser);
        }

        public async Task<IEnumerable<Book>> CheckChaptersInBookList(Book book, CancellationToken token, WebScraperBrowser? browser = null)
        {
            browser ??= new WebScraperBrowser();
            List<Book> books = await App.Database.GetBooksByBooklistIdAsync(book.BookListId);
            List<Book> filteredBooks = books.FindAll((filterBook) => !filterBook.HasNextChapter && filterBook.Id != book.Id);
            return await CheckChaptersInBookList(filteredBooks, token, browser);
        }

        public async Task<IEnumerable<Book>> CheckChaptersInBookList(List<Book> books, CancellationToken token, WebScraperBrowser browser) {
            List<Book> verifiedbooks = new();

            foreach (Book book in books) {
                if (token.IsCancellationRequested) return verifiedbooks;

                browser.Source = book.Url;
                string html = await browser.GetHtml();
                Config? config = await WebService.FindValidConfig(book.Url, html);
                if (config is null || (bool)config.ExtraConfigs.GetValueOrDefault("webview", false)) continue;

                var nextElementExists = await browser.WaitUntilValid(new(config.NextUrlPath), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
                if (!nextElementExists) continue;

                HtmlDocument doc = new();
                doc.LoadHtml(await browser.GetHtml());
                var nextUrl = ConfigService.PrettyWrapSelector(doc, new(config.NextUrlPath), ConfigService.SelectorType.Link);

                book.HasNextChapter = nextUrl.Length > 0;
                verifiedbooks.Add(book);
            }
            return verifiedbooks;
        }
    }
}
