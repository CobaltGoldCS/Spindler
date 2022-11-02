namespace Spindler.Models;

public class GeneralizedConfig : Config
{
    /// <summary>
    /// The path to check in order to see if this configuration is valid
    /// </summary>
    public string MatchPath { get; set; }
}
