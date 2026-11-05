using SharpQuest.Contracts;

namespace Capstone.Core.Models;

public sealed class Drink : MenuItem
{
    public Size Size { get; }

    public Drink(
        string id,
        string displayName,
        Category category,
        Money price,
        Size size = Size.Medium,
        Allergen allergens = Allergen.None,
        bool isAvailable = true)
        : base(id, displayName, category, price, allergens, isAvailable)
    {
        if (!Enum.IsDefined(size))
            throw new ArgumentOutOfRangeException(nameof(size), "Unknown size");
        Size = size;
    }

    public override string Describe() =>
        $"{Card()} · {Size} drink · {Price.Amount:0.00} {Price.Currency} · {(IsAvailable ? "available" : "unavailable")}";

    public override MenuItem WithAvailability(bool isAvailable) =>
        new Drink(Id, DisplayName, Category, Price, Size, Allergens, isAvailable);

    public override MenuItem WithPrice(Money price) =>
        new Drink(Id, DisplayName, Category, price, Size, Allergens, IsAvailable);
}