using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json;
using Spindler.Services;
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
        }
        #endregion

        protected enum State
        {
            NewConfig,
            ModifyConfig
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
                FileSaverImplementation fileSaverInstance = new();
                var filePath = await fileSaverInstance.SaveAsync($"{Configuration.Name.Replace('.', '-')}.json", stream, cancellationToken);
                await Toast.Make($"File saved at {filePath}").Show(cancellationToken);
#if IOS || MACCATALYST
            fileSaverInstance.Dispose();
#endif
            }
            catch (Exception ex)
            {
                await Toast.Make($"File not saved: {ex.Message}").Show(cancellationToken);
            }
        }

        protected virtual async Task Import(object sender, EventArgs e)
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
        }

        #endregion

        public BaseConfigDetailPage() 
        {
            
        }
    }
}
