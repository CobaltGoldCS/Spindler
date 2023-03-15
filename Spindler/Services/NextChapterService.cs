using HtmlAgilityPack;
using Spindler.CustomControls;
using Spindler.Models;

namespace Spindler.Services {
    public class NextChapterService 
    {
        public NextChapterService() { }

        public async Task Run() {
            List<BookList> bookLists = await App.Database.GetBookListsAsync();
            Task[] processes = new Task[bookLists.Count];

            int i = 0;
            foreach (BookList bookList in bookLists) {
                processes[i] = CheckChaptersInBookList(bookList);
                i++;
            }
            var superProcess = Task.WhenAll(processes);
            await superProcess;
        }

        private async Task CheckChaptersInBookList(BookList bookList) {
            List<Book> books = await App.Database.GetBooksByBooklistIdAsync(bookList.Id);
            WebScraperBrowser webScraperBrowser = WebScraperBrowser.CreateHeadless();
            foreach (Book book in books) {
                if (book.HasNextChapter) continue;

                webScraperBrowser.Source = book.Url;
                var html = await webScraperBrowser.GetHtml();
                Config? config = await WebService.FindValidConfig(book.Url, html);
                if (config is null || (bool)config.ExtraConfigs.GetValueOrDefault("webview", false)) continue;

                var nextElementExists = await webScraperBrowser.WaitUntilValid(new(config.NextUrlPath), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
                if (!nextElementExists) continue;

                HtmlDocument doc = new();
                doc.LoadHtml(await webScraperBrowser.GetHtml());
                var nextUrl = ConfigService.PrettyWrapSelector(doc, new(config.NextUrlPath), ConfigService.SelectorType.Link);

                book.HasNextChapter = nextUrl.Length > 0;
                await App.Database.SaveItemAsync(book);
            }
        }
    }
}
