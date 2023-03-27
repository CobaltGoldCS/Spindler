using Spindler.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Utilities
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Execute either an add or remove operation on observablecollection in order to 
        /// update the <paramref name="baseCollection"/> to match <paramref name="targetValues"/> while preserving other values like scroll
        /// </summary>
        /// <typeparam name="T">Any type that can be stored by an IEnumerable</typeparam>
        /// <param name="baseCollection">The <see cref="ObservableCollection{T}"/> to operate on</param>
        /// <param name="targetValues">The items that <paramref name="baseCollection"/> should contain</param>
        public static void ExecuteAddAndDeleteTransactions<T>(this ObservableCollection<T> baseCollection, IEnumerable<T> targetValues) where T : IIndexedModel
        {
            var targetIds = new HashSet<int>(targetValues.Select(p => p.GetId()));
            IList<T> valuesToDelete = baseCollection.Where(item => !targetValues.Contains(item)).ToImmutableArray();

            foreach (T value in valuesToDelete)
            {
                baseCollection.Remove(value);
            }

            var baseIds = new HashSet<int>(baseCollection.Select(item => item.GetId()));
            IList itemsToAdd = targetValues.Where(p => !baseIds.Contains(p.GetId())).ToImmutableArray();

            foreach (T value in itemsToAdd)
            {
                baseCollection.Insert(0, value);
            }
        }
    }
}
