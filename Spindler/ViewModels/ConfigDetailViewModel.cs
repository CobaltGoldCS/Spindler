using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Spindler.Models;
using Spindler.Services;
using Spindler.Services.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spindler.ViewModels;

public partial class ConfigDetailViewModel : ObservableObject
{

    private IDataService Database { get; init; }

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

    private ContentExtractorOption[] possibleExtractors = ((TargetType[]) Enum.GetValues(typeof(TargetType)))
           .Select(ContentExtractorOption.FromContentType)
           .ToArray();

    public ContentExtractorOption[] PossibleExtractors
    {
        get => possibleExtractors;
    }



    [ObservableProperty]
    public string? separatorText;

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


    public ConfigDetailViewModel(IDataService database)
    {
        Database = database;
    }

    #region Event Handlers

    [RelayCommand]
    public async Task Delete()
    {
        if (Config.Id == -1 || !await Shell.Current.CurrentPage!.DisplayAlert("Warning!", "Are you sure you want to delete this config?", "Yes", "No")) return;
        if (IsGeneralizedConfig())
        {
            await Database.DeleteItemAsync((GeneralizedConfig)Config);
        }
        else
        {
            await Database.DeleteItemAsync((Config)Config);
        }
        await Shell.Current.GoToAsync("..");
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

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task Cancel() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    public async Task Export()
    {
        CancellationToken cancellationToken = new();

        string output = JsonConvert.SerializeObject(Config);
        using MemoryStream stream = new(Encoding.Default.GetBytes(output));

        try
        {
            var filePath = await FileSaver.Default.SaveAsync($"{Config.Name.Replace('.', '-')}.json", stream, cancellationToken);
            filePath.EnsureSuccess();
            await Toast.Make($"File saved at {filePath}").Show(cancellationToken);
        }
        catch (Exception ex)
        {
            await Toast.Make($"File not saved: {ex.Message}").Show(cancellationToken);
        }
    }


    [RelayCommand]
    public async Task Import()
    {
        CancellationToken cancellationToken = new();
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
            await Toast.Make("File not saved: could not convert JSON to string").Show(cancellationToken);
            return;
        }
        Config = config;
        Config.Id = -1; // Required to create a new config
    }
    #endregion

    [GeneratedRegex("^(?!www\\.)(((?!\\-))(xn\\-\\-)?[a-z0-9\\-_]{0,61}[a-z0-9]{1,1}\\.)*(xn\\-\\-)?([a-z0-9\\-]{1,61}|[a-z0-9\\-]{1,30})\\.[a-z]{2,}$")]
    private static partial Regex DomainValidation();
}
