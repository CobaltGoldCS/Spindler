using Spindler.Services.Web;

namespace Spindler.Models;

public class GeneralizedConfig : Config
{
    /// <summary>
    /// The path to check in order to see if this configuration is valid
    /// </summary>
    public string MatchPath { get; set; } = "";

    public override bool IsValidConfig() => DomainName != String.Empty
        && SelectionService.IsValidSelector(MatchPath)
        && SelectionService.IsValidSelector(ContentPath)
        && SelectionService.IsValidSelector(NextUrlPath)
        && SelectionService.IsValidSelector(PrevUrlPath);
}
