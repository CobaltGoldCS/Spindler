using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json;
using Spindler.Services;
using Spindler.Services.Web;
using System.Text;

namespace Spindler.Views.Configuration_Pages
{
    /// <summary>
    /// A Base Class for Config Detail Pages, implementing non-page specific behaviors
    /// <para> See <seealso cref="ConfigDetailPage"/> and <seealso cref="GeneralizedConfigDetailPage"/> </para>
    /// </summary>
    /// <typeparam name="TConfig">Must be derived from <code>Config</code></typeparam>
    public abstract class BaseConfigDetailPage<TConfig> : ContentPage, IQueryAttributable where TConfig : Models.Config, new()
    {
        #region Attributes 

        protected ContentExtractorOption[] possibleExtractors = ((TargetType[])Enum.GetValues(typeof(TargetType)))
        .Select(content => ContentExtractorOption.FromContentType(content))
        .ToArray();

        protected ContentExtractorOption selectedExtractor = ContentExtractorOption.FromContentType(TargetType.Text);

        protected State state = State.NewConfig;

        protected TConfig configuration = new() { Id = -1 };
        protected TConfig Configuration
        {
            get => configuration;
            set
            {
                configuration = value;
                OnPropertyChanged();
            }
        }

        public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Configuration = (query["config"] as TConfig)!;
            BindingContext = Configuration;
            if (Configuration.Id < 0)
            {
                state = State.NewConfig;
            }
            else
            {
                state = State.ModifyConfig;
            }
            selectedExtractor = possibleExtractors.FirstOrDefault(extractor => Convert.ToInt32(extractor.contentType) == Configuration.ContentType, possibleExtractors[0]);
            SetSwitchesBasedOnExtraConfigs();
        }
        #endregion

        protected enum State
        {
            NewConfig,
            ModifyConfig
        }

        protected class ContentExtractorOption
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


        #region Click Handlers
        protected virtual async void DeleteButton_Clicked(object sender, EventArgs e)
        {
            if (state == State.NewConfig || !await DisplayAlert("Warning!", "Are you sure you want to delete this config?", "Yes", "No")) return;
            await App.Database.DeleteItemAsync(Configuration);
            await Shell.Current.GoToAsync("..");
        }

        protected virtual async void okButton_Clicked(object sender, EventArgs e)
        {
            if (!ConfigService.IsValidSelector(Configuration.ImageUrlPath))
                Configuration.ImageUrlPath = "";

            await App.Database.SaveItemAsync(Configuration);
            await Shell.Current.GoToAsync("..");
        }

        protected virtual async void Cancel_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");

        protected async void ExportCommand(object sender, EventArgs e)
        {
            CancellationToken cancellationToken = new();

            string output = JsonConvert.SerializeObject(Configuration);
            using MemoryStream stream = new(Encoding.Default.GetBytes(output));

            try
            {
                var filePath = await FileSaver.Default.SaveAsync($"{Configuration.Name.Replace('.', '-')}.json", stream, cancellationToken);
                filePath.EnsureSuccess();
                await Toast.Make($"File saved at {filePath}").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"File not saved: {ex.Message}").Show(cancellationToken);
            }
        }

        /// <summary>
        /// Holds all of the logic required to import a configuration except for UI
        /// </summary>
        /// <param name="sender">The Button that triggered the task</param>
        /// <param name="e">The Event</param>
        /// <returns>A task to await</returns>
        protected virtual async void ImportCommand(object sender, EventArgs e)
        {
            CancellationToken cancellationToken = new();
            FileResult? file = await FilePicker.Default.PickAsync(PickOptions.Default);

            if (file is null) return;

            Stream contents = await file.OpenReadAsync();
            MemoryStream stream = new();
            await contents.CopyToAsync(stream);

            string JSON = Encoding.Default.GetString(stream.ToArray());
            TConfig? config = JsonConvert.DeserializeObject<TConfig>(JSON);
            if (config is null)
            {
                await Toast.Make("File not saved: could not convert JSON to string").Show(cancellationToken);
                return;
            }
            Configuration = config;
            BindingContext = Configuration;
            Configuration.Id = -1; // Required to create a new config
            SetSwitchesBasedOnExtraConfigs();
        }

        #endregion

        public BaseConfigDetailPage()
        {

        }

        /// <summary>
        /// UI Logic to set controls based on configuration
        /// </summary>
        protected abstract void SetSwitchesBasedOnExtraConfigs();
    }
}
