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
        bool isAvailable = true,
        bool isFeatured = false,
        int preparationTimeMinutes = 5,
        string ? sku = null,
        VatInfo? vat = null) 
        : this(id, displayName, category, price, CheckedParts(parts), isAvailable,  isFeatured,  preparationTimeMinutes, sku, vat)
    {
    }

    // The combo contains every allergen of its parts, and is only available when all of them are.
    private Combo(
        string id,
        string displayName,
        Category category,
        Money price,
        List<MenuItem> parts,
        bool isAvailable,
        bool isFeatured = false,
        int preparationTimeMinutes = 5, 
        string ? sku = null,
        VatInfo? vat = null)
        : base(string.IsNullOrWhiteSpace(sku) && sku is not null
            ? throw new ArgumentException($"Sku '{sku}' is invalid.", nameof(sku))
            : sku ?? $"COMBO-{id}",
            id, displayName, category, price,
            parts.Aggregate(Allergen.None, (all, part) => all | part.Allergens),
            isAvailable && parts.All(part => part.IsAvailable),
            isFeatured,  preparationTimeMinutes,vat)
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

    public override int EstimatedPreparationMinutes =>
        Parts.Sum(part => part.EstimatedPreparationMinutes);

    public override MenuItem WithAvailability(bool isAvailable) =>
        new Combo(Id, DisplayName, Category, Price, Parts, isAvailable, IsFeatured, PreparationTimeMinutes, Sku, Vat);

    public override MenuItem WithPrice(Money price) =>
        new Combo(Id, DisplayName, Category, price, Parts, IsAvailable, IsFeatured,  PreparationTimeMinutes, Sku, Vat);
}