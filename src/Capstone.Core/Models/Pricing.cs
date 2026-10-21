namespace Capstone.Core.Models;

public static class Pricing
{
    private static readonly TimeOnly HalfPriceFrom = new(18, 0);

    /// <summary>The price to charge at a given time of day, rounded to the cent.</summary>
    public static decimal FinalPrice(MenuItem item, TimeOnly time)
    {
        var price = item switch
        {
            null => throw new ArgumentNullException(nameof(item)),
            Drink { Size: Size.Large } => item.Price.Amount * 1.20m, // large drink + 20 %
            Food when time >= HalfPriceFrom => item.Price.Amount * 0.5m, //half price after 6 o clock
            _ => item.Price.Amount,
        };

        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }
}