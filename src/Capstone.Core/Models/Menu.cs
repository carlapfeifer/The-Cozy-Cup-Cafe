using SharpQuest.Contracts;

namespace Capstone.Core.Models;

public class MenuItem : IItem,IPriced
{
    public string Id { get; }
    public string DisplayName { get; }
    public Category Category { get; }
    public Money Price { get; }
    public Size? Size { get; }            
    public Allergen Allergens { get; }
    public bool IsAvailable { get; }

    public MenuItem(
        string id,
        string displayName,
        Category category,
        Money price,
        Size? size = null,
        Allergen allergens = Allergen.None,
        bool isAvailable = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(price);

        if (price.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

        Id = id;
        DisplayName = displayName;
        Category = category;
        Price = price;
        Size = size;
        Allergens = allergens;
        IsAvailable = isAvailable;
    }
}