namespace Capstone.Core.Models;

public sealed class Food : MenuItem
{
    public Food(
        string id,
        string displayName,
        Category category,
        Money price,
        Allergen allergens = Allergen.None,
        bool isAvailable = true,
        bool isFeatured = false,
        int preparationTimeMinutes = 5,
        string? sku = null,
        VatInfo? vat = null,
        TimeOnly? availableFrom = null,
        TimeOnly? availableUntil = null)
        : base(string.IsNullOrWhiteSpace(sku) && sku is  not null
            ? throw new ArgumentException($"Sku '{sku}' is invalid.", nameof(sku))
            : sku ?? $"FOOD-{id}",
            id, displayName, category, price, allergens, isAvailable,  isFeatured,  preparationTimeMinutes, vat,
            availableFrom, availableUntil)
    {
        if (preparationTimeMinutes is < 3 or > 8)
            throw new ArgumentOutOfRangeException(
                nameof(preparationTimeMinutes),
                "Food preparation time must be between 3 and 8 minutes.");
    }

    public override int EstimatedPreparationMinutes =>
        PreparationTimeMinutes;

    public override string Describe() =>
        Allergens == Allergen.None
            ? $"{Card()} · food · no allergens · {Price.Amount:0.00} {Price.Currency} · {(IsAvailable ? "available" : "unavailable")}"
            : $"{Card()} · food · contains {Allergens} · {Price.Amount:0.00} {Price.Currency} · {(IsAvailable ? "available" : "unavailable")}";

    public override MenuItem WithAvailability(bool isAvailable) =>
        new Food(Id, DisplayName, Category, Price, Allergens, isAvailable, IsFeatured, PreparationTimeMinutes, Sku, Vat,
            AvailableFrom, AvailableUntil);

    public override MenuItem WithPrice(Money price) =>
        new Food(Id, DisplayName, Category, price, Allergens, IsAvailable, IsFeatured, PreparationTimeMinutes, Sku, Vat,
            AvailableFrom, AvailableUntil);
}