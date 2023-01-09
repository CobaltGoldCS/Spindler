namespace Spindler.Models;

/// <summary>
/// A simple interface denoting an indexed model for the sqlite database
/// </summary>
public interface IIndexedModel
{
    /// <summary>
    /// Gets the UID associated with a given model in the sqlite database
    /// </summary>
    /// <returns>The UID associated with a given model</returns>
    int GetId();

    /// <summary>
    /// Gets a formatted name for display
    /// </summary>
    string Name
    {
        get;
    }
}
