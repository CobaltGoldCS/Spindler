using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Utilities;

static class UiUtilities
{
    /// <summary>
    /// Populate <paramref name="collection"/> with <paramref name="values"/> while informing the UI of the change
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    /// <param name="collection">The collection to clear and populate</param>
    /// <param name="values">Values to insert into collection</param>
    /// <param name="shouldClear">Whether the collection should be cleared before it is populated</param>
    public static void PopulateAndNotify<T>(this ObservableCollection<T> collection, IEnumerable<T> values, bool shouldClear = false)
    {
        if (shouldClear)
        {
            collection.Clear();
        }

        foreach (T value in values)
        {
            collection.Add(value);
        }
    }
}
