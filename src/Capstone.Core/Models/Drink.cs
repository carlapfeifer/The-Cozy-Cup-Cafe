using SharpQuest.Contracts;

namespace Capstone.Core.Models;

public sealed class Drink : MenuItem
{
    public Size Size { get; }
    public bool IsHot { get; }

    public Drink(
        string id,
        string displayName,
        Category category,
        Money price,
        Size size = Size.Medium,
        Allergen allergens = Allergen.None,
        bool isAvailable = true,
        bool isFeatured = false,
        bool isHot = true,
        int preparationTimeMinutes = 5,
        string? sku = null,
        VatInfo? vat = null,
        bool isDecaf = false,
        TimeOnly? availableFrom = null,
        TimeOnly? availableUntil = null)
        : base(string.IsNullOrWhiteSpace(sku) && sku is not null 
            ? throw new  ArgumentException($"Sku '{sku}' is invalid.", nameof(sku))
            : sku ?? $"DRINK-{id}",
            id, displayName, category, price, allergens, isAvailable, isFeatured, preparationTimeMinutes, vat,
            availableFrom, availableUntil)
    {
        if (!Enum.IsDefined(size))
            throw new ArgumentOutOfRangeException(nameof(size), "Unknown size");
        Size = size;
        IsHot = isHot;
        IsDecaf = isDecaf;
    }

    public bool IsDecaf { get; }

    public override int EstimatedPreparationMinutes => IsHot ? 2 : 1;

    public override string Describe() =>
        $"{Card()} · {Size} drink · {Price.Amount:0.00} {Price.Currency} · {(IsAvailable ? "available" : "unavailable")}";

    public override MenuItem WithAvailability(bool isAvailable) =>
        new Drink(Id, DisplayName, Category, Price, Size, Allergens, isAvailable, IsFeatured, IsHot,
            PreparationTimeMinutes, Sku, Vat, IsDecaf, AvailableFrom, AvailableUntil);

    public override MenuItem WithPrice(Money price) =>
        new Drink(Id, DisplayName, Category, price, Size, Allergens, IsAvailable, IsFeatured, IsHot,
            PreparationTimeMinutes, Sku, Vat, IsDecaf, AvailableFrom, AvailableUntil);
}