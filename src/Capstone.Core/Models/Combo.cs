namespace Capstone.Core.Models;

public sealed class Combo : MenuItem
{
    public IReadOnlyList<MenuItem> Parts { get; }

    public Combo(
        string id,
        string displayName,
        Category category,
        Money price,
        IEnumerable<MenuItem> parts,
        bool isAvailable = true)
        : this(id, displayName, category, price, CheckedParts(parts), isAvailable)
    {
    }

    // The combo contains every allergen of its parts, and is only available when all of them are.
    private Combo(
        string id,
        string displayName,
        Category category,
        Money price,
        List<MenuItem> parts,
        bool isAvailable)
        : base(id, displayName, category, price,
            parts.Aggregate(Allergen.None, (all, part) => all | part.Allergens),
            isAvailable && parts.All(part => part.IsAvailable))
    {
        Parts = parts.AsReadOnly();
    }

    private static List<MenuItem> CheckedParts(IEnumerable<MenuItem> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var list = parts.ToList();
        if (list.Count == 0)
            throw new ArgumentException("A combo needs at least one part.", nameof(parts));
        if (list.Any(p => p is null))
            throw new ArgumentException("A combo cannot contain a null part.", nameof(parts));

        return list;
    }

    public override string Describe() =>
        $"{Card()} · combo of {string.Join(" + ", Parts.Select(p => p.DisplayName))} · {Price.Amount:0.00} {Price.Currency}";

    public override MenuItem WithAvailability(bool isAvailable) =>
        new Combo(Id, DisplayName, Category, Price, Parts, isAvailable);

    public override MenuItem WithPrice(Money price) =>
        new Combo(Id, DisplayName, Category, price, Parts, IsAvailable);
}