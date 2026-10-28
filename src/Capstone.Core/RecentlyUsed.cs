namespace Capstone.Core;

/// <summary>The last N distinct values, newest first.</summary>
public sealed class RecentlyUsed<T>
{
    private readonly List<T> _values = new();   // index 0 is the newest

    public int Capacity { get; }

    public int Count => _values.Count;

    public RecentlyUsed(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");

        Capacity = capacity;
    }

    public void Push(T value)
    {
        var comparer = EqualityComparer<T>.Default;   // works for any T, unlike ==

        var existing = _values.FindIndex(v => comparer.Equals(v, value));
        if (existing >= 0)
            _values.RemoveAt(existing);               // moves to the front, not a second place

        _values.Insert(0, value);

        if (_values.Count > Capacity)
            _values.RemoveAt(_values.Count - 1);      // the oldest drops out
    }

    /// <summary>The values, newest first.</summary>
    public IEnumerable<T> Items() => _values.ToArray();
}