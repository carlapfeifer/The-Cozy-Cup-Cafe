using System.Collections;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core;

public sealed record ItemAddedChange(IItem Item, int TotalCount);

public sealed record ItemUpdatedChange(IItem PreviousItem, IItem UpdatedItem);

public sealed record AvailabilityChangedChange(IItem Item, bool PreviousAvailability, bool IsAvailable);

public sealed record PriceChangedChange(IItem Item, Money PreviousPrice, Money Price);

public class CafeStore : IItemStore, IEnumerable<IItem>
{
    private readonly List<IItem> _items = new();

    private readonly Dictionary<string, IItem> _byId = new();

    public int Count => _items.Count;

    /// <summary>Raised after an item has really been added, once per item.</summary>
    public event Action<IItem>? ItemAdded;

    public event EventHandler<ItemAddedChange>? ItemAddedWithDetails;
    public event Action<IItem>? ItemRemoved;
    public event EventHandler<ItemUpdatedChange>? ItemUpdated;
    public event EventHandler<AvailabilityChangedChange>? AvailabilityChanged;
    public event EventHandler<PriceChangedChange>? PriceChanged;

    //menu item position
    public IItem this[int index] => _items[index];


    //menu item by ID
    //returns null if item doesn't exist
    public IItem? this[string id] => id is null ? null : _byId.GetValueOrDefault(id);


    public void Add(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_byId.ContainsKey(item.Id))
            throw new ArgumentException($"Item with id {item.Id} already exists");

        _byId.Add(item.Id, item);
        _items.Add(item);

        ItemAdded?.Invoke(item);
        ItemAddedWithDetails?.Invoke(this, new ItemAddedChange(item, Count));
    }

    public IItem? FindById(string id) => this[id];
    public IReadOnlyList<IItem> All() => _items.ToArray();

    public bool Remove(string id)
    {
        if (id is null || !_byId.Remove(id, out var item))
            return false;
        _items.Remove(item);
        ItemRemoved?.Invoke(item);
        return true;
    }

    public bool Update(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!_byId.TryGetValue(item.Id, out var previousItem))
            return false;

        var index = _items.FindIndex(existingItem => existingItem.Id == item.Id);
        _items[index] = item;
        _byId[item.Id] = item;

        ItemUpdated?.Invoke(this, new ItemUpdatedChange(previousItem, item));
        return true;
    }

    public bool SetAvailability(string id, bool isAvailable)
    {
        if (id is null)
            return false;

        if (!_byId.TryGetValue(id, out var item))
            return false;

        if (item is not MenuItem menuItem)
            throw new InvalidOperationException($"Item {id} does not support availability changes.");

        if (menuItem.IsAvailable == isAvailable)
            return true;

        var updatedItem = menuItem.WithAvailability(isAvailable);
        if (updatedItem.IsAvailable != isAvailable)
            throw new InvalidOperationException(
                $"Item {id} cannot be marked available while one or more combo parts are unavailable.");

        Update(updatedItem);
        AvailabilityChanged?.Invoke(
            this,
            new AvailabilityChangedChange(updatedItem, menuItem.IsAvailable, updatedItem.IsAvailable));
        return true;
    }

    public bool SetPrice(string id, Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        if (price.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (id is null)
            return false;

        if (!_byId.TryGetValue(id, out var item))
            return false;

        if (item is not MenuItem menuItem)
            throw new InvalidOperationException($"Item {id} does not support price changes.");

        if (menuItem.Price == price)
            return true;

        var updatedItem = menuItem.WithPrice(price);
        Update(updatedItem);
        PriceChanged?.Invoke(this, new PriceChangedChange(updatedItem, menuItem.Price, updatedItem.Price));
        return true;
    }


    public IEnumerator<IItem> GetEnumerator()
    {
        foreach (var item in _items)
        {
            yield return item;
        }
    }


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<IItem> Matching(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return MatchingIterator(text);
    }

    private IEnumerable<IItem> MatchingIterator(string text)
    {
        foreach (var item in _items)
        {
            if (text.Length == 0 || item.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                yield return item;
            }
        }
    }
}