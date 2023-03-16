using CommunityToolkit.Maui.Alerts;
using HtmlAgilityPack;
using Spindler.CustomControls;
using Spindler.Models;

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
        /// Checks all the books in a booklist with <paramref name="id"/> for their next chapter and updates the database
        /// </summary>
        /// <param name="id">The id of the target booklist</param>
        /// <param name="browser">A webscraper browser instance</param>
        /// <param name="token">A token to cancel the operation if needed</param>
        /// <returns></returns>
        public async Task CheckChaptersInBookList(int id, CancellationToken token, WebScraperBrowser? browser = null) {
            browser ??= new WebScraperBrowser();
            List<Book> books = await App.Database.GetBooksByBooklistIdAsync(id);
            foreach (Book book in books) {
                if (token.IsCancellationRequested) return;

                if (book.HasNextChapter) continue;

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
                await App.Database.SaveItemAsync(book);
            }
        }
    }
}
