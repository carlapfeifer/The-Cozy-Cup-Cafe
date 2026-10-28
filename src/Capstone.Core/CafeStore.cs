using System.Collections;
using SharpQuest.Contracts;

namespace Capstone.Core;

public class CafeStore : IItemStore,IEnumerable<IItem>
{
    private readonly List<IItem> _items = new();

    private readonly Dictionary<string, IItem> _byId = new();
    
    public int Count => _items.Count;
    
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
    }

    public IItem? FindById(string id) => this[id];
    public IReadOnlyList<IItem> All() => _items.ToArray();

    public bool Remove(string id)
    {
        if (id is null || !_byId.Remove(id, out var item))
            return false;
        _items.Remove(item);
        return true;
    }

    
    
    public IEnumerator<IItem> GetEnumerator()
    {
        foreach (var item  in _items)
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