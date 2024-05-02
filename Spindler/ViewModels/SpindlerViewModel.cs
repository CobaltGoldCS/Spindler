using CommunityToolkit.Mvvm.ComponentModel;
using Spindler.Services;
using System.Diagnostics.CodeAnalysis;

namespace Spindler.ViewModels;

public abstract class SpindlerViewModel : ObservableObject
{
    private IDataService? database;
    protected WeakReference<Page> CurrentPage
    {
        get => lazyPage.Value;
    }
    private Lazy<WeakReference<Page>> lazyPage { get; set; }

    /// <summary>
    /// Database Handler. Will throw if constructor is passed a null database parameter.
    /// </summary>
    [NotNullIfNotNull(nameof(database))]
    protected IDataService Database {
        get
        {
            ArgumentNullException.ThrowIfNull(nameof(database));
            return database!;
        }
        set => database = value;
    }

    protected SpindlerViewModel(IDataService? database)
    {
        if (database != null)
        {
            Database = database;
        }
        lazyPage = new Lazy<WeakReference<Page>>(() => new WeakReference<Page>(Shell.Current.CurrentPage));
    }

#nullable disable
    protected static async Task NavigateTo(string route)
    {
        await NavigateTo(route, new Dictionary<string, object>());
    }

    protected static async Task NavigateTo(string route, IDictionary<string, object> parameters, bool animated = true)
    {
        await Shell.Current.GoToAsync(route, animated, parameters: parameters);
    }
#nullable enable
}
