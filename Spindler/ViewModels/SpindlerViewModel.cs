using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Services;
using System.Diagnostics.CodeAnalysis;

namespace Spindler.ViewModels;

public abstract partial class SpindlerViewModel : ObservableObject
{
    private IDataService? database;
    protected WeakReference<Page> CurrentPage
    {
        get => LazyPage.Value;
    }
    private Lazy<WeakReference<Page>> LazyPage { get; set; }

    /// <summary>
    /// Database Handler. Will throw if constructor is passed a null database parameter.
    /// </summary>
    [NotNullIfNotNull(nameof(database))]
    protected IDataService Database
    {
        get
        {
            ArgumentNullException.ThrowIfNull(database, nameof(database));
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
        LazyPage = new Lazy<WeakReference<Page>>(() => new WeakReference<Page>(Shell.Current.CurrentPage));
    }

#nullable disable

    protected async Task NavigateTo(string route)
    {
        await NavigateTo(route, new Dictionary<string, object>());
    }

    /// <summary>
    /// Overridable Navigation Function.<br></br>
    /// To override Navigation Completely, use Shell.BackButtonCommand with the Back Command defined in this class.
    /// </summary>
    /// <returns>Task Related to Navigating with Shell</returns>
    protected virtual async Task NavigateTo(string route, IDictionary<string, object> parameters, bool animated = true)
    {
        await Shell.Current.GoToAsync(route, animated, parameters: parameters);
    }

    [RelayCommand]
    public virtual async Task Back() => await NavigateTo("..");
#nullable enable
}
