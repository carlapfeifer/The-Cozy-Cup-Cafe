using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Subscribers;

/// <summary>Counts how many items were added to each category (Coffee, Pastries, ...).</summary>
public sealed class CategoryBoard
{
    private readonly Dictionary<string, int> _tally = new();

    public IReadOnlyDictionary<string, int> Tally => _tally;

    /// <summary>Starts listening; returns the function that stops listening.</summary>
    public Action Attach(IItemStore store) => StoreFunctions.Subscribe(store, OnItemAdded);

    private void OnItemAdded(IItem item)
    {
        if (item is not MenuItem menuItem)
            return;   // only menu items have a category

        var name = menuItem.Category.Name;
        _tally[name] = _tally.GetValueOrDefault(name) + 1;
    }
}