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
        VatInfo? vat = null,
        TimeOnly? availableFrom = null,
        TimeOnly? availableUntil = null,
        IEnumerable<string>? ingredients = null,
        ItemOrigin origin = ItemOrigin.MadeInHouse)
        : this(id, displayName, category, price, CheckedParts(parts), isAvailable, isFeatured,
            preparationTimeMinutes, sku, vat, availableFrom, availableUntil, ingredients, origin)
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
        VatInfo? vat = null,
        TimeOnly? availableFrom = null,
        TimeOnly? availableUntil = null,
        IEnumerable<string>? ingredients = null,
        ItemOrigin origin = ItemOrigin.MadeInHouse)
        : base(string.IsNullOrWhiteSpace(sku) && sku is not null
            ? throw new ArgumentException($"Sku '{sku}' is invalid.", nameof(sku))
            : sku ?? $"COMBO-{id}",
            id, displayName, category, price,
            parts.Aggregate(Allergen.None, (all, part) => all | part.Allergens),
            isAvailable && parts.All(part => part.IsAvailable),
            isFeatured, preparationTimeMinutes, vat, availableFrom, availableUntil,
            ingredients ?? parts.SelectMany(p => p.Ingredients).Distinct(StringComparer.OrdinalIgnoreCase),
            origin)
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

    public override bool IsAvailableAt(TimeOnly time) =>
        base.IsAvailableAt(time) && Parts.All(part => part.IsAvailableAt(time));

    public override MenuItem WithAvailability(bool isAvailable) =>
        new Combo(Id, DisplayName, Category, Price, Parts, isAvailable, IsFeatured, PreparationTimeMinutes, Sku, Vat,
            AvailableFrom, AvailableUntil, Ingredients, Origin);

    public override MenuItem WithPrice(Money price) =>
        new Combo(Id, DisplayName, Category, price, Parts, IsAvailable, IsFeatured, PreparationTimeMinutes, Sku, Vat,
            AvailableFrom, AvailableUntil, Ingredients, Origin);

    public Combo WithParts(IEnumerable<MenuItem> parts) =>
        new(Id, DisplayName, Category, Price, parts, IsAvailable, IsFeatured, PreparationTimeMinutes, Sku, Vat,
            AvailableFrom, AvailableUntil, Ingredients, Origin);
}