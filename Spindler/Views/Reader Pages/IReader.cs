namespace Spindler.Views.Reader_Pages
{
    public interface IReader
    {
        /// <summary>
        /// Display <paramref name="message"/> if <paramref name="value"/> is <c>null</c>
        /// </summary>
        /// <param name="value">The value to check for nullability</param>
        /// <param name="message">The message to display</param>
        /// <returns>If the object is null or not</returns>
        protected Task<bool> FailIfNull(object? value, string message);

        /// <summary>
        /// Handler for when Shell Navigates to a new page
        /// </summary>
        /// <param name="sender">The view that triggered the navigation</param>
        /// <param name="e">The navigating arguments</param>
        protected void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e);
    }
}
