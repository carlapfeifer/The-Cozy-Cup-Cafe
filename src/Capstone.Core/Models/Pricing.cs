namespace Capstone.Core.Models;

public static class Pricing
{
    public static decimal FinalPrice(MenuItem item, TimeOnly time)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.FinalPrice(time);
    }
}