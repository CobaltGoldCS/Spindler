using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Spindler.Models;
using Spindler.Services;
using Spindler.Services.Web;
using System.Text;

namespace Spindler.ViewModels;

public partial class ConfigDetailViewModel(IDataService database) : SpindlerViewModel(database)
{

    public class ContentExtractorOption
    {
        public string name;
        public TargetType contentType;

        private ContentExtractorOption(string name, TargetType contentType)
        {
            this.name = name;
            this.contentType = contentType;
        }

        public static ContentExtractorOption FromContentType(TargetType type)
        {
            return new ContentExtractorOption(Enum.GetName(typeof(TargetType), type)!, type);
        }

        public override string ToString()
        {
            return name.Replace("_", " ");
        }
    }

    Config? config = null;

    public Config Config
    {
        get => config!;
        set
        {
            config = value;
            OnPropertyChanged();
        }
    }

    public string? MatchPath
    {
        get => (config as GeneralizedConfig)?.MatchPath;
        set
        {
            (config as GeneralizedConfig)!.MatchPath = value!;
            OnPropertyChanged();
        }
    }

    private ContentExtractorOption[] possibleExtractors = ((TargetType[])Enum.GetValues(typeof(TargetType)))
           .Select(ContentExtractorOption.FromContentType)
           .ToArray();

    public ContentExtractorOption[] PossibleExtractors
    {
        get => possibleExtractors;
    }



    [ObservableProperty]
    public string separatorText = string.Empty;

    [ObservableProperty]
    public ContentExtractorOption selectedExtractor = ContentExtractorOption.FromContentType(TargetType.Text);



    public void SetConfig(Config config)
    {
        Config = config;

        SeparatorText = config.Separator
            .Replace(Environment.NewLine, @"\n")
            .Replace("\t", @"\t");

        SelectedExtractor = PossibleExtractors
            .FirstOrDefault(extractor => Convert.ToInt32(extractor.contentType) == Config.ContentType, PossibleExtractors[0]);
    }

    public bool IsGeneralizedConfig() => Config is GeneralizedConfig;

    #region Event Handlers

    [RelayCommand]
    public async Task Delete()
    {
        if (Config.Id == -1 || (CurrentPage.TryGetTarget(out Page? currentPage) &&
            !await currentPage!.DisplayAlert("Warning!", "Are you sure you want to delete this config?", "Yes", "No")))
            return;

        if (IsGeneralizedConfig())
        {
            await Database.DeleteItemAsync((GeneralizedConfig)Config);
        }
        else
        {
            await Database.DeleteItemAsync(Config);
        }
        await NavigateTo("..");
    }

    [RelayCommand]
    public async Task Add()
    {
        if (!Config.IsValidConfig())
        {
            await Toast.Make("Please make sure to have valid arguments").Show();
            return;
        }

        if (!ConfigService.IsValidSelector(Config.ImageUrlPath))
            Config.ImageUrlPath = "";

        Config.Separator = SeparatorText
            .Replace(@"\n", Environment.NewLine)
            .Replace(@"\t", "\t");

        await Database.SaveItemAsync(Config);

        await NavigateTo("..");
    }

    [RelayCommand]
    public static async Task Cancel() => await NavigateTo("..");

    [RelayCommand]
    public async Task Export()
    {
        string output = JsonConvert.SerializeObject(Config);
        using MemoryStream stream = new(Encoding.Default.GetBytes(output));

        try
        {
            var filePath = await FileSaver.Default.SaveAsync($"{Config.Name.Replace('.', '-')}.json", stream);
            filePath.EnsureSuccess();
            await Toast.Make($"File saved at {filePath}").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"File not saved: {ex.Message}").Show();
        }
    }


    [RelayCommand]
    public async Task Import()
    {
        FileResult? file = await FilePicker.Default.PickAsync(PickOptions.Default);

        if (file is null) return;

        Stream contents = await file.OpenReadAsync();
        MemoryStream stream = new();
        await contents.CopyToAsync(stream);

        string JSON = Encoding.Default.GetString(stream.ToArray());
        Config? config;
        if (IsGeneralizedConfig())
        {
            config = JsonConvert.DeserializeObject<GeneralizedConfig>(JSON);
        }
        else
        {
            config = JsonConvert.DeserializeObject<Config>(JSON);
        }
        if (config is null)
        {
            await Toast.Make("File not saved: could not convert JSON to string").Show();
            return;
        }
        Config = config;
        Config.Id = -1; // Required to create a new config
    }
    #endregion
}
