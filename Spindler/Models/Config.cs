using Newtonsoft.Json;
using SQLite;

namespace Spindler.Models;

/// <summary>
/// A Model representing configurations between the sqlite database and the backend code
/// </summary>
public class Config : IIndexedModel
{
    /// <summary>
    /// UID of the Config
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int GetId() => Id;

    /// <summary>
    /// The domain name associated with the config (IE: example.com)
    /// </summary>
    public string DomainName { get; set; } = "";

    /// <summary>
    /// The path pointing to the title element (if specified) otherwise null
    /// </summary>
    public string TitlePath { get; set; } = "";

    /// <summary>
    /// The path pointing to the main content of the website
    /// </summary>
    public string ContentPath { get; set; } = "";

    /// <summary>
    /// The path pointing to the url denoting the "next chapter"
    /// </summary>
    public string NextUrlPath { get; set; } = "";

    /// <summary>
    /// The path pointing to the url denoting the "previous chapter"
    /// </summary>
    public string PrevUrlPath { get; set; } = "";


    private string _pathType = "";
    /// <summary>
    /// The type of path that is denoted in the configuration (usually xpath)
    /// </summary>
    public string PathType { get => _pathType; private set => _pathType = value; }

    /// <summary>
    /// The extra configs in string form. Use <see ref="ExtraConfigs"/> instead
    /// </summary>
    public string ExtraConfigsBlobbed { get; set; } = "";

    /// <summary>
    /// A dictionary containing extra configuration settings
    /// </summary>
    [Ignore]
    public Dictionary<string, object> ExtraConfigs
    {
        get => JsonConvert.DeserializeObject<Dictionary<string, object>>(ExtraConfigsBlobbed) ?? new();
        set
        {
            ExtraConfigsBlobbed = JsonConvert.SerializeObject(value);
        }
    }
    public void SetPathType(string pathType)
    {
        PathType = pathType;
    }
}
