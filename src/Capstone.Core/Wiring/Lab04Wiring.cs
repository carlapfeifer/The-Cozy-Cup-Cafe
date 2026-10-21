using System.Reflection;
using Capstone.Core.Models;
using SharpQuest.Contracts;


namespace Capstone.Core.Wiring;

public sealed class Lab04Wiring : ILab04Wiring
{
    private static readonly Type[] Kinds = { typeof(Drink), typeof(Food) };
    public IItemStore CreateStore() => new CafeStore();

    /// <summary>Your abstract base class.</summary>
    public Type BaseType => typeof(MenuItem);
   
    /// <summary>
    /// Your concrete kinds, two or more, each deriving from <see cref="BaseType"/>.
    /// The order is yours; <see cref="CreateOfKind"/> uses the same order.
    /// </summary>
    public IReadOnlyList<Type> KindTypes => Kinds;

    /// <summary>
    /// A valid item of <c>KindTypes[kindIndex]</c>.
    /// The same kind and seed always give the same Id; different kinds or different seeds give different Ids.
    /// Kinds 0 and 1 and seeds 1 to 100 are used.
    /// </summary>
    public IItem CreateOfKind(int kindIndex, int seed) => kindIndex switch
    {
        0 => new Drink($"drink-{seed}", $"Drink {seed}",
            new Category("cof", "Coffee"), new Money(10m, "RON"), Size.Medium),
        1 => new Food($"food-{seed}", $"Food {seed}",
            new Category("cake", "Cakes"), new Money(15m, "RON"), Allergen.Gluten),
        _ => throw new ArgumentOutOfRangeException(nameof(kindIndex)),
    };

    /// <summary>
    /// The value of the polymorphic member for this item — call it through the base type and let the
    /// override decide. Never empty. Two kinds built from the same seed must describe themselves differently.
    /// </summary>
    public string Describe(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return ((MenuItem)item).Describe();   // the override decides
    }

    /// <summary>
    /// One description per item in the store, in store order, each equal to <see cref="Describe"/> of that item.
    /// Write it as a single loop over the base type: the loop must not know which kind it is holding.
    /// An empty store gives an empty list. Describing never changes the store.
    /// </summary>
    public IReadOnlyList<string> DescribeAll(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var lines = new List<string>();
        foreach (var item in store.All().Cast<MenuItem>())
            lines.Add(item.Describe());       // no if, no type test
        return lines;
    }

    /// <summary>
    /// A short label saying what this item is — written as a <b>switch expression</b> over type patterns.
    /// <list type="bullet">
    /// <item>Items of the same kind get the same label; different kinds get different labels.</item>
    /// <item>An item that is not one of your kinds (such as <see cref="ForeignItem"/>) gets a fallback
    ///       label, different from every kind label. That's the <c>_ =&gt;</c> arm.</item>
    /// <item><c>null</c> is a programming error: <see cref="ArgumentNullException"/>. That's the
    ///       <c>null =&gt; throw</c> arm.</item>
    /// </list>
    /// </summary>
    public string Classify(IItem item) => item switch
    {
        null => throw new ArgumentNullException(nameof(item)),
        Drink => "drink",
        Food => "food",
        _ => "something else",
    };

    /// <summary>
    /// Looks for an item whose DisplayName is exactly <paramref name="name"/> (case-sensitive).
    /// True and the item when it's there; false and <c>null</c> when it isn't.
    /// A null or blank name is simply not found — a Try method never throws for that.
    /// Never changes the store.
    /// </summary>
    public bool TryFindByName(IItemStore store, string name, out IItem? found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        found = store.All().FirstOrDefault(i => i.DisplayName == name);
        return found is not null;
    }

    /// <summary>
    /// Adds every item, in order, and returns how many were added. Declared with <c>params</c>,
    /// so <c>AddAll(store)</c>, <c>AddAll(store, a)</c> and <c>AddAll(store, a, b, c)</c> all compile.
    /// The store's own rules still apply: a null item or a duplicate Id throws, and whatever was added
    /// before the bad one stays added.
    /// </summary>
    public int AddAll(IItemStore store, params IItem[] items)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(items);

        var added = 0;
        foreach (var item in items)
        {
            store.Add(item);                  // null or duplicate throws; earlier items stay added
            added++;
        }
        return added;
        
    }
}