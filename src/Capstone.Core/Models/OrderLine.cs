namespace Capstone.Core.Models;

/// <summary>One line of an order: a menu item and how many of it.</summary>
public sealed class OrderLine
{
    public MenuItem Item { get; }
    public int Quantity { get; }

    public OrderLine(MenuItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");

        Item = item;
        Quantity = quantity;
    }

    /// <summary>Unit price times quantity (not rounded).</summary>
    public decimal Total => Item.Price.Amount * Quantity;
}