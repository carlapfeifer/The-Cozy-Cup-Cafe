using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core;

/// <summary>Ready-made rules for StoreFunctions.Where. Each one takes a setting and RETURNS a function that remembers it.</summary>
public static class MenuFilters
{
    /// <summary>Items that cost at most <paramref name="max"/> (the limit itself is included).</summary>
    public static Func<IItem, bool> AtMostPrice(decimal max) =>
        item => item is MenuItem menuItem && menuItem.Price.Amount <= max;
}