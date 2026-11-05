using SharpQuest.Contracts;

namespace Capstone.Core.Subscribers;

/// <summary>The "menu board": counts how many items of each kind were added.</summary>
public sealed class MenuBoard
{
    private readonly Dictionary<string, int> _tally = new();

    public IReadOnlyDictionary<string, int> Tally => _tally;

    /// <summary>Starts listening; returns the function that stops listening.</summary>
    public Action Attach(IItemStore store) => StoreFunctions.Subscribe(store, OnItemAdded);

    private void OnItemAdded(IItem item)
    {
        var kind = item.GetType().Name;
        _tally[kind] = _tally.GetValueOrDefault(kind) + 1;
    }
}