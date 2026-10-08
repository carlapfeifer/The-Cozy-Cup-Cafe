using SharpQuest.Contracts;

namespace Capstone.Core;

public class CafeStore : IItemStore
{
    private readonly List<IItem> _items = new();
    
    public int Count => _items.Count;

    public void Add(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        

        if (_items.Any(i => i.Id == item.Id))
        {
            throw new ArgumentException($"Item with id {item.Id} already exists");
        }
        _items.Add(item);
    }

    public IItem? FindById(string id)
    {
        return _items.FirstOrDefault(i => i.Id == id);
    }

    public IReadOnlyList<IItem> All()
    {
        return _items.ToArray();
    }

    public bool Remove(string id)
    {
        int index = _items.FindIndex(i => i.Id == id);
        if (index < 0) return false;
        
        _items.RemoveAt(index);
        return true;
    }
    
    
}