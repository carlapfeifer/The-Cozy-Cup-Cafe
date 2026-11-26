namespace Capstone.Core.Models;

/// <summary>One line of an order: a menu item and how many of it.</summary>
public sealed class OrderLine
{
    public MenuItem Item { get; }
    public int Quantity { get; }
    public MilkOption Milk { get; }
    public SyrupOption Syrup { get; }
    public decimal CustomizationSurcharge =>
        (Milk == MilkOption.Regular ? 0m : 2m)
        + (Syrup == SyrupOption.None ? 0m : 3m);
    public string CustomizationSummary => Item is not Drink
        ? ""
        : string.Join(", ", new[]
        {
            Milk == MilkOption.Regular ? null : $"{Milk} milk",
            Syrup == SyrupOption.None ? null : $"{Syrup} syrup"
        }.Where(value => value is not null));

    public OrderLine(
        MenuItem item,
        int quantity,
        MilkOption milk = MilkOption.Regular,
        SyrupOption syrup = SyrupOption.None)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");
        if (!Enum.IsDefined(milk))
            throw new ArgumentOutOfRangeException(nameof(milk));
        if (!Enum.IsDefined(syrup))
            throw new ArgumentOutOfRangeException(nameof(syrup));
        if (item is not Drink && (milk != MilkOption.Regular || syrup != SyrupOption.None))
            throw new ArgumentException("Milk and syrup options are only available for drinks.");

        Item = item;
        Quantity = quantity;
        Milk = milk;
        Syrup = syrup;
    }
    
}