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
    
    public bool IsFeatured { get; }
    public int PreparationTimeMinutes { get; }
    public string Sku { get; }   //stock keeping unit - short code for ordering, inventory, receipts 
    public VatInfo Vat { get; }
    public TimeOnly? AvailableFrom { get; }
    public TimeOnly? AvailableUntil { get; }
    public IReadOnlyList<string> Ingredients { get; }
    public ItemOrigin Origin { get; }

    protected MenuItem(
        string sku,
        string id,
        string displayName,
        Category category,
        Money price,
        // Size? size = null,
        Allergen allergens = Allergen.None,
        bool isAvailable = true,
        bool isFeatured = false,
        int preparationTimeMinutes = 5,
        VatInfo? vat = null,
        TimeOnly? availableFrom = null,
        TimeOnly? availableUntil = null,
        IEnumerable<string>? ingredients = null,
        ItemOrigin origin = ItemOrigin.MadeInHouse
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(price);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        if (price.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (preparationTimeMinutes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(preparationTimeMinutes), "Preparation time must be at least one minute .");
        }
        if (availableFrom.HasValue != availableUntil.HasValue)
            throw new ArgumentException("Both availability window times must be provided together.");
        if (availableFrom == availableUntil && availableFrom.HasValue)
            throw new ArgumentException("Availability window must have a non-zero duration.");
        
        Sku = sku;
        Id = id;
        DisplayName = displayName;
        Category = category;
        Price = price;
        //Size = size;
        Allergens = allergens;
        IsAvailable = isAvailable;
        IsFeatured = isFeatured;
        PreparationTimeMinutes = preparationTimeMinutes;
        Vat = vat ?? new VatInfo(11m, 11m);
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        Ingredients = ingredients is null
            ? Array.Empty<string>()
            : ingredients.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Origin = origin;
    }

    /// <summary>
    /// every kind must describe itself
    /// </summary>
    /// <returns></returns>
    public abstract string Describe();

    /// <summary>Estimated minutes needed to prepare this kind of menu item.</summary>
    public abstract int EstimatedPreparationMinutes { get; }

    public abstract MenuItem WithAvailability(bool isAvailable);
    public abstract MenuItem WithPrice(Money price);

    public virtual bool IsAvailableAt(TimeOnly time)
    {
        if (!IsAvailable)
            return false;
        if (AvailableFrom is null)
            return true;

        return AvailableFrom.Value < AvailableUntil!.Value
            ? time >= AvailableFrom.Value && time < AvailableUntil!.Value
            : time >= AvailableFrom.Value || time < AvailableUntil!.Value;
    }

    public string AvailabilityWindow => AvailableFrom is null
        ? ""
        : $"{AvailableFrom:HH:mm}–{AvailableUntil:HH:mm}";

    public string OriginDisplay => Origin == ItemOrigin.SupplierBought ? "Supplier Bought" : "Made in House";

    private static readonly TimeOnly HalfPriceFrom = new(18, 0);

    public decimal FinalPrice(TimeOnly time)
    {
        var amount = this switch
        {
            Drink { Size: Size.Large } => Price.Amount * 1.20m,
            Food when time >= HalfPriceFrom => Price.Amount * 0.50m,
            _ => Price.Amount
        };

        return Math.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// shared part of every description
    /// </summary>
    /// <returns></returns>
    protected string Card() => $"{Id} {DisplayName}";
}