using SharpQuest.Contracts;

namespace Capstone.Core.Models;

public abstract class MenuItem : IItem, IPriced
{
    public string Id { get; }
    public string DisplayName { get; }
    public Category Category { get; }

    public Money Price { get; }

    //public Size? Size { get; }            
    public Allergen Allergens { get; }
    public bool IsAvailable { get; }

    protected MenuItem(
        string id,
        string displayName,
        Category category,
        Money price,
        // Size? size = null,
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
        //Size = size;
        Allergens = allergens;
        IsAvailable = isAvailable;
    }

    /// <summary>
    /// every kind must describe itself
    /// </summary>
    /// <returns></returns>
    public abstract string Describe();

    public abstract MenuItem WithAvailability(bool isAvailable);
    public abstract MenuItem WithPrice(Money price);

    /// <summary>
    /// shared part of every description
    /// </summary>
    /// <returns></returns>
    protected string Card() => $"{Id} {DisplayName}";
}