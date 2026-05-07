namespace Spindler.Models;

public class GeneralizedConfig : Config
{
    /// <summary>
    /// The path to check in order to see if this configuration is valid
    /// </summary>
    public string MatchPath { get; set; } = "";

    public override bool IsValidConfig() => DomainName != string.Empty
        && MatchPath.AsPath().IsValid()
        && ContentPath.AsPath().IsValid()
        && NextUrlPath.AsPath().IsValid()
        && PrevUrlPath.AsPath().IsValid();
}
