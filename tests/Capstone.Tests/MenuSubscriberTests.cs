using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Subscribers;

namespace Capstone.Tests;

public class MenuSubscriberTests
{
    private static readonly Category Coffee = new("cof", "Coffee");

    private static Drink MakeDrink(
        string id,
        string name,
        bool isAvailable = true,
        bool isFeatured = false) =>
        new(
            id,
            name,
            Coffee,
            new Money(12m, "RON"),
            isAvailable: isAvailable,
            isFeatured: isFeatured);

    [Fact]
    public void SalesLog_records_sold_out_items()
    {
        var store = new CafeStore();
        var salesLog = new SalesLog();
        using var subscription = salesLog.Attach(store);

        var drink = MakeDrink("latte", "Latte");
        store.Add(drink);
        store.SetAvailability(drink.Id, false);

        var soldOutItem = Assert.Single(salesLog.SoldOutItems);
        Assert.Equal(drink.Id, soldOutItem.Id);
        Assert.False(((MenuItem)soldOutItem).IsAvailable);
    }

    [Fact]
    public void AvailabilityTransitionLog_records_unavailable_to_available_changes()
    {
        var store = new CafeStore();
        var log = new AvailabilityTransitionLog();
        using var subscription = log.Attach(store);

        store.Add(MakeDrink("latte", "Latte", isAvailable: false));
        store.SetAvailability("latte", true);
        store.SetAvailability("latte", false);

        var restocked = Assert.Single(log.RestockedItems);
        Assert.Equal("latte", restocked.Id);
        Assert.True(restocked.IsAvailable);
    }

    [Fact]
    public void FeaturedMenuCache_refreshes_after_a_menu_update()
    {
        var store = new CafeStore();
        var cache = new FeaturedMenuCache();
        using var subscription = cache.Attach(store);

        store.Add(MakeDrink("latte", "Latte", isFeatured: true));
        Assert.Equal(
            new[] { "latte" },
            cache.GetFeaturedItems(store).Select(item => item.Id));

        store.Update(MakeDrink("latte", "Latte", isFeatured: false));

        Assert.Empty(cache.GetFeaturedItems(store));
    }

    [Fact]
    public void CategorySalesCounter_counts_line_quantities_after_order_completion()
    {
        var store = new CafeStore();
        var counter = new CategorySalesCounter();
        using var subscription = counter.Attach(store);

        var latte = MakeDrink("latte", "Latte");
        var order = new Order();
        order.Add(latte, 2);

        store.NotifyOrderCompleted(
            order,
            new Money(24m, "RON"),
            DateTime.Today);

        Assert.Equal(2, counter.UnitsSold["cof"]);
    }

    [Fact]
    public void ReceiptMemory_stores_a_compact_receipt_after_order_completion()
    {
        var store = new CafeStore();
        var receiptMemory = new ReceiptMemory();
        using var subscription = receiptMemory.Attach(store);

        var order = new Order();
        order.Add(MakeDrink("latte", "Latte"), 2);

        store.NotifyOrderCompleted(
            order,
            new Money(4.80m, "EUR"),
            DateTime.Today.AddHours(12));

        var receipt = Assert.Single(receiptMemory.Receipts);
        Assert.Contains("2x Latte", receipt);
        Assert.Contains("Paid 4.80 EUR", receipt);
    }
}