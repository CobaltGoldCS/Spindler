using Spindler.Utilities;

namespace Spindler.Services.Web
{
    public interface IWebService
    {
        /// <summary>
        /// Gets html provided by a url, or the error html
        /// </summary>
        /// <param name="url">The Uniform Resource Locator pointing to a website</param>
        /// <returns>Success or failure html relating to the request</returns>
        public Task<Result<string>> GetHtmlFromUrl(string url, CancellationToken? token = null);
    }
}
