using System.Globalization;
using System.Text;
using Capstone.Core.Models;

namespace Capstone.Core.Storage;

public static class CafeReportFormatter
{
    public static string FormatReceipt(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var text = new StringBuilder()
            .AppendLine("THE COZY CUP CAFE")
            .AppendLine("ORDER RECEIPT")
            .AppendLine(order.PlacedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
            .AppendLine($"Service: {FormatServiceMode(order.ServiceMode)}")
            .AppendLine($"Payment: {FormatPaymentMethod(order.PaymentMethod)}")
            .AppendLine(new string('-', 32));

        foreach (var line in order.Lines)
        {
            var lineTotal = order.UnitPriceOf(line) * line.Quantity;
            text.AppendLine($"{line.Quantity} x {line.Item.DisplayName}");
            if (line.CustomizationSummary.Length > 0)
                text.AppendLine($"  {line.CustomizationSummary}");
            text.AppendLine($"  {lineTotal:0.00} {order.Currency}");
        }

        text.AppendLine(new string('-', 32))
            .AppendLine($"Subtotal: {order.Subtotal:0.00} {order.Currency}");

        if (order.DiscountPercent > 0)
        {
            text.AppendLine(
                $"Discount ({order.DiscountPercent:0.##}%): -{order.Subtotal - order.Total:0.00} {order.Currency}");
        }

        text.AppendLine($"TOTAL: {order.Total:0.00} {order.Currency}");
        text.AppendLine($"VAT included: {order.VatTotal:0.00} {order.Currency}");
        foreach (var vat in order.VatBreakdown.OrderBy(entry => entry.Key))
        {
            text.AppendLine(
                $"  VAT {vat.Key:0.##}%: {vat.Value:0.00} {order.Currency}");
        }

        return text.ToString();
    }

    public static string FormatEndOfDay(
        IEnumerable<Order> orders,
        DateOnly businessDate)
    {
        ArgumentNullException.ThrowIfNull(orders);

        var todaysOrders = orders
            .Where(order => DateOnly.FromDateTime(order.PlacedAt) == businessDate)
            .OrderBy(order => order.PlacedAt)
            .ToArray();

        var text = new StringBuilder()
            .AppendLine("THE COZY CUP CAFE")
            .AppendLine($"END-OF-DAY REPORT — {businessDate:yyyy-MM-dd}")
            .AppendLine(new string('=', 40))
            .AppendLine("DAILY SUMMARY")
            .AppendLine($"Completed orders: {todaysOrders.Length}")
            .AppendLine($"Items sold: {todaysOrders.Sum(order => order.Lines.Sum(line => line.Quantity))}");

        foreach (var currencyGroup in todaysOrders.GroupBy(order => order.Currency))
        {
            var total = currencyGroup.Sum(order => order.Total);
            text.AppendLine($"Total revenue: {total:0.00} {currencyGroup.Key}");
            text.AppendLine($"Average order: {total / currencyGroup.Count():0.00} {currencyGroup.Key}");
        }

        var bestSellers = MenuReports.SalesCountPerMenuItem(todaysOrders)
            .OrderByDescending(item => item.UnitsSold)
            .ThenBy(item => item.ItemName, StringComparer.Ordinal)
            .Take(5)
            .ToArray();
        text.AppendLine().AppendLine("MOST ORDERED ITEMS");
        if (bestSellers.Length == 0)
            text.AppendLine("No items sold.");
        else
            foreach (var item in bestSellers)
                text.AppendLine($"{item.ItemName}: {item.UnitsSold} sold");

        text.AppendLine().AppendLine("REVENUE BY PAYMENT METHOD");
        foreach (var payment in todaysOrders
                     .GroupBy(order => new { order.PaymentMethod, order.Currency })
                     .OrderBy(group => group.Key.PaymentMethod)
                     .ThenBy(group => group.Key.Currency, StringComparer.Ordinal))
        {
            text.AppendLine(
                $"{FormatPaymentMethod(payment.Key.PaymentMethod)}: {payment.Sum(order => order.Total):0.00} {payment.Key.Currency}");
        }

        text.AppendLine().AppendLine("REVENUE BY HOUR");
        foreach (var hour in todaysOrders
                     .GroupBy(order => new { order.PlacedAt.Hour, order.Currency })
                     .OrderBy(group => group.Key.Hour)
                     .ThenBy(group => group.Key.Currency, StringComparer.Ordinal))
        {
            text.AppendLine($"{hour.Key.Hour:00}:00 — {hour.Sum(order => order.Total):0.00} {hour.Key.Currency}");
        }

        text.AppendLine().AppendLine(new string('=', 40)).AppendLine("ORDER DETAILS");
        text.AppendLine();
        for (var index = 0; index < todaysOrders.Length; index++)
        {
            var order = todaysOrders[index];
            text.AppendLine($"Order {index + 1} — {order.PlacedAt:HH:mm}");
            foreach (var line in order.Lines)
            {
                var customization = line.CustomizationSummary.Length == 0
                    ? ""
                    : $" ({line.CustomizationSummary})";
                text.AppendLine(
                    $"  {line.Quantity} x {line.Item.DisplayName}{customization} — {order.UnitPriceOf(line) * line.Quantity:0.00} {order.Currency}");
            }

            text.AppendLine($"  Total: {order.Total:0.00} {order.Currency}");
            text.AppendLine();
        }

        return text.ToString();
    }

    private static string FormatServiceMode(ServiceMode serviceMode) =>
        serviceMode == ServiceMode.Takeaway ? "Takeaway" : "Dine-in";

    private static string FormatPaymentMethod(PaymentMethod paymentMethod) =>
        paymentMethod switch
        {
            PaymentMethod.MobileWallet => "Mobile wallet",
            _ => paymentMethod.ToString()
        };
}
