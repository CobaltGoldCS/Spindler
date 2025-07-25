using Spindler.Services.Web;

namespace Spindler.Models;

public class GeneralizedConfig : Config
{
    /// <summary>
    /// The path to check in order to see if this configuration is valid
    /// </summary>
    public string MatchPath { get; set; } = "";

    public override bool IsValidConfig() => DomainName != String.Empty 
        && ConfigService.IsValidSelector(MatchPath)
        && ConfigService.IsValidSelector(ContentPath)
        && ConfigService.IsValidSelector(NextUrlPath)
        && ConfigService.IsValidSelector(PrevUrlPath);
}
