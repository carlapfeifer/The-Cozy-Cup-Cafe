using System.Collections;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core;

public sealed record ItemAddedChange(IItem Item, int TotalCount);

public sealed record ItemUpdatedChange(IItem PreviousItem, IItem UpdatedItem);

public sealed record AvailabilityChangedChange(IItem Item, bool PreviousAvailability, bool IsAvailable);

public sealed record PriceChangedChange(IItem Item, Money PreviousPrice, Money Price);
public sealed record ItemSoldOutChange(IItem Item);

public sealed record OrderCompletedChange(
    Order Order,
    Money Total,
    DateTime CompletedAt);

public sealed record MenuChangedChange(string Change, IItem Item);

public class CafeStore : IItemStore, IEnumerable<IItem>
{
    private readonly List<IItem> _items = new();

    private readonly Dictionary<string, IItem> _byId = new();
    
    private readonly Dictionary<string, List<IItem>> _byCategory =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count => _items.Count;

    /// <summary>Raised after an item has really been added, once per item.</summary>
    public event Action<IItem>? ItemAdded;

    public event EventHandler<ItemAddedChange>? ItemAddedWithDetails;
    public event Action<IItem>? ItemRemoved;
    public event EventHandler<ItemUpdatedChange>? ItemUpdated;
    public event EventHandler<AvailabilityChangedChange>? AvailabilityChanged;
    public event EventHandler<PriceChangedChange>? PriceChanged;
    public event EventHandler<ItemSoldOutChange>? ItemSoldOut;
    public event EventHandler<OrderCompletedChange>? OrderCompleted;
    public event EventHandler<MenuChangedChange>? MenuChanged;

    //menu item position
    public IItem this[int index] => _items[index];


    //menu item by ID
    //returns null if item doesn't exist
    public IItem? this[string id] => id is null ? null : _byId.GetValueOrDefault(id);
    
    
    public IItem? First => _items.Count == 0 ? null : _items[0];

    public IItem? Last => _items.Count == 0 ? null : _items[^1];

    public int AvailableCount =>
        _items.Count(item => item is MenuItem { IsAvailable: true });




    public void Add(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_byId.ContainsKey(item.Id))
            throw new ArgumentException($"Item with id {item.Id} already exists");

        _byId.Add(item.Id, item);
        _items.Add(item);
        AddToCategoryIndex(item);

        ItemAdded?.Invoke(item);
        ItemAddedWithDetails?.Invoke(this, new ItemAddedChange(item, Count));
        MenuChanged?.Invoke(this, new MenuChangedChange("Added", item));
    }

    public IItem? FindById(string id) => this[id];
    public IReadOnlyList<IItem> All() => _items.ToArray();

    public bool Remove(string id)
    {
        if (id is null || !_byId.Remove(id, out var item))
            return false;
        _items.Remove(item);
        RemoveFromCategoryIndex(item);
        ItemRemoved?.Invoke(item);
        MenuChanged?.Invoke(this, new MenuChangedChange("Removed", item));
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
        RemoveFromCategoryIndex(previousItem);
        AddToCategoryIndex(item);

        ItemUpdated?.Invoke(this, new ItemUpdatedChange(previousItem, item));
        MenuChanged?.Invoke(this, new MenuChangedChange("Updated", item));
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
        if (!updatedItem.IsAvailable)
            ItemSoldOut?.Invoke(this, new ItemSoldOutChange(updatedItem));
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

    // A fresh, ordered snapshot each time this property is read.
    public IReadOnlyList<IItem> AvailableItems =>
        _items.Where(item => item is MenuItem { IsAvailable: true }).ToArray();

    public int CountByCategory(string categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);

        return _byCategory.TryGetValue(categoryId, out var items)
            ? items.Count
            : 0;
    }

    public bool ContainsCategory(string categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        return _byCategory.ContainsKey(categoryId);
    }

// The category's Id is used for lookup. Results preserve menu order.
    public IReadOnlyList<IItem> this[Category category]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(category);

            return _byCategory.TryGetValue(category.Id, out var items)
                ? items.ToArray()
                : Array.Empty<IItem>();
        }
    }
    
    
    private void AddToCategoryIndex(IItem item)
    {
        if (item is not MenuItem menuItem)
            return;

        if (!_byCategory.TryGetValue(menuItem.Category.Id, out var categoryItems))
        {
            categoryItems = new List<IItem>();
            _byCategory.Add(menuItem.Category.Id, categoryItems);
        }

        var itemPosition = _items.FindIndex(existing => existing.Id == item.Id);
        var insertAt = categoryItems.FindIndex(existing =>
            _items.FindIndex(menuItemInStore => menuItemInStore.Id == existing.Id) > itemPosition);

        if (insertAt < 0)
            categoryItems.Add(item);
        else
            categoryItems.Insert(insertAt, item);
    }

    private void RemoveFromCategoryIndex(IItem item)
    {
        if (item is not MenuItem menuItem
            || !_byCategory.TryGetValue(menuItem.Category.Id, out var categoryItems))
        {
            return;
        }

        categoryItems.RemoveAll(existing => existing.Id == item.Id);

        if (categoryItems.Count == 0)
            _byCategory.Remove(menuItem.Category.Id);
    }
    
    public MenuSubscription SubscribeItemAdded(Action<IItem> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ItemAdded += handler;
        return new MenuSubscription(() => ItemAdded -= handler);
    }

    public MenuSubscription SubscribeItemSoldOut(EventHandler<ItemSoldOutChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ItemSoldOut += handler;
        return new MenuSubscription(() => ItemSoldOut -= handler);
    }

    public MenuSubscription SubscribeOrderCompleted(EventHandler<OrderCompletedChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        OrderCompleted += handler;
        return new MenuSubscription(() => OrderCompleted -= handler);
    }

    public MenuSubscription SubscribeMenuChanged(EventHandler<MenuChangedChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        MenuChanged += handler;
        return new MenuSubscription(() => MenuChanged -= handler);
    }

    public void NotifyOrderCompleted(Order order, Money total, DateTime completedAt)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(total);

        if (order.Lines.Count == 0)
            throw new InvalidOperationException("An empty order cannot be completed.");

        OrderCompleted?.Invoke(
            this,
            new OrderCompletedChange(order, total, completedAt));
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