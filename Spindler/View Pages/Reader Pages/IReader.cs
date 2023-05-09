namespace Spindler.Views.Reader_Pages
{
    public interface IReader
    {
        /// <summary>
        /// Display assert that <paramref name="condition"/> is true, or gracefully fail
        /// </summary>
        /// <param name="condition">The condition to evaluate</param>
        /// <param name="message">The message to display if the condition is not met</param>
        /// <returns>Whether the assertion succeeded or not</returns>
        protected Task<bool> SafeAssert(bool condition, string message);

        /// <summary>
        /// Assert that <paramref name="value"/> is not null, or gracefully fail
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="message">The error to pass to the user</param>
        /// <returns>True if the value is not null, or false if it is </returns>
        public Task<bool> SafeAssertNotNull(object? value, string message) => SafeAssert(value is not null, message);

        /// <summary>
        /// Handler for when Shell Navigates to a new page
        /// </summary>
        /// <param name="sender">The view that triggered the navigation</param>
        /// <param name="e">The navigating arguments</param>
        protected void OnShellNavigating(object? sender,
                           ShellNavigatingEventArgs e);
    }
}
