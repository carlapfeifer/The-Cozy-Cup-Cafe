namespace Capstone.Core.Models;

public sealed class Food : MenuItem
{
    public Food(
        string id,
        string displayName,
        Category category,
        Money price,
        Allergen allergens = Allergen.None,
        bool isAvailable = true)
        : base(id, displayName, category, price, allergens, isAvailable)
    {
    }

    public override string Describe() =>
        Allergens == Allergen.None
            ? $"{Card()} · food · no allergens · {Price.Amount:0.00} {Price.Currency} · {(IsAvailable ? "available" : "unavailable")}"
            : $"{Card()} · food · contains {Allergens} · {Price.Amount:0.00} {Price.Currency} · {(IsAvailable ? "available" : "unavailable")}";

    public override MenuItem WithAvailability(bool isAvailable) =>
        new Food(Id, DisplayName, Category, Price, Allergens, isAvailable);

    public override MenuItem WithPrice(Money price) =>
        new Food(Id, DisplayName, Category, price, Allergens, IsAvailable);
}