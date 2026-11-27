namespace Capstone.Core.Models;

/// <summary>One line of an order: a menu item and how many of it.</summary>
public sealed class OrderLine
{
    public MenuItem Item { get; }
    public int Quantity { get; }
    public MilkOption Milk { get; }
    public SyrupOption Syrup { get; }
    public SugarOption Sugar { get; }
    public BreadOption Bread { get; }

    public decimal CustomizationSurcharge =>
        (Milk == MilkOption.Regular ? 0m : 2m)
        + (Syrup == SyrupOption.None ? 0m : 3m);

    public string CustomizationSummary
    {
        get
        {
            var parts = new List<string>();
            if (Milk != MilkOption.Regular)
                parts.Add($"{Milk} milk");
            if (Syrup != SyrupOption.None)
                parts.Add($"{Syrup} syrup");
            if (Sugar != SugarOption.None)
                parts.Add(Sugar == SugarOption.Honey ? "Honey" : $"{Sugar} sugar");
            if (Bread != BreadOption.Standard)
                parts.Add($"{Bread} bread");

            return string.Join(", ", parts);
        }
    }

    public OrderLine(
        MenuItem item,
        int quantity,
        MilkOption milk = MilkOption.Regular,
        SyrupOption syrup = SyrupOption.None,
        SugarOption sugar = SugarOption.None,
        BreadOption bread = BreadOption.Standard)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");
        if (!Enum.IsDefined(milk))
            throw new ArgumentOutOfRangeException(nameof(milk));
        if (!Enum.IsDefined(syrup))
            throw new ArgumentOutOfRangeException(nameof(syrup));
        if (!Enum.IsDefined(sugar))
            throw new ArgumentOutOfRangeException(nameof(sugar));
        if (!Enum.IsDefined(bread))
            throw new ArgumentOutOfRangeException(nameof(bread));

        Item = item;
        Quantity = quantity;
        Milk = milk;
        Syrup = syrup;
        Sugar = sugar;
        Bread = bread;
    }
}
