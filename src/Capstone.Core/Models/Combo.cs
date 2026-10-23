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
        : base(id, displayName, category, price, CombineAllergens(parts), isAvailable)
    {
        Parts = parts.ToList();
    }

    // The combo contains every allergen of its parts.
    private static Allergen CombineAllergens(IEnumerable<MenuItem> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var list = parts.ToList();
        if (list.Count == 0)
            throw new ArgumentException("A combo needs at least one part.", nameof(parts));
        if (list.Any(p => p is null))
            throw new ArgumentException("A combo cannot contain a null part.", nameof(parts));

        return list.Aggregate(Allergen.None, (all, part) => all | part.Allergens);
    }

    public override string Describe() =>
        $"{Card()} · combo of {string.Join(" + ", Parts.Select(p => p.DisplayName))} · {Price.Amount:0.00} {Price.Currency}";
}