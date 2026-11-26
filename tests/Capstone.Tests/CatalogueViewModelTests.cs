using Capstone.Core.Wiring;
using Capstone.Core.Currency;
using Capstone.Core.Models;
using Capstone.Core.Storage;
using Capstone.Core.ViewModels;

namespace Capstone.Tests;

public class CatalogueViewModelTests
{
    private sealed class FakeRates(decimal rate) : IExchangeRateProvider
    {
        public Task<decimal> GetRateAsync(
            string from,
            string to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(rate);
    }

    [Fact]
    public void Order_commands_update_quantity_lines_and_total()
    {
        var wiring = new Lab09Wiring();
        var store = wiring.CreateStore();
        var item = wiring.CreateSample(1);
        store.Add(item);

        var viewModel = (CatalogueViewModel)wiring.CreateViewModel(store);

        viewModel.AddToOrderCommand.Execute(item);
        viewModel.AddToOrderCommand.Execute(item);

        Assert.Same(item, Assert.Single(viewModel.MostOrderedItems));
        var line = Assert.Single(viewModel.OrderLines);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(20m, viewModel.OrderTotal);

        viewModel.SelectedOrderLine = line;
        viewModel.RemoveOneOrderItemCommand.Execute(null);

        line = Assert.Single(viewModel.OrderLines);
        Assert.Equal(1, line.Quantity);
        Assert.Equal(10m, viewModel.OrderTotal);
        Assert.Contains(item.DisplayName, viewModel.ReceiptPreview);

        viewModel.RemoveOrderLineCommand.Execute(null);

        Assert.Empty(viewModel.OrderLines);
        Assert.Equal(0m, viewModel.OrderTotal);
    }

    [Fact]
    public void Menu_is_grouped_by_category_and_category_selection_filters_items()
    {
        var wiring = new Lab09Wiring();
        var store = wiring.CreateStore();
        var item = wiring.CreateSample(1);
        store.Add(item);
        var menuItem = Assert.IsAssignableFrom<MenuItem>(item);

        var viewModel = (CatalogueViewModel)wiring.CreateViewModel(store);

        var section = Assert.Single(viewModel.MenuSections);
        Assert.Equal(menuItem.Category.Name, section.Name);
        Assert.Same(menuItem, Assert.Single(section.Items));

        viewModel.SelectCategoryCommand.Execute(section.Name);
        viewModel.Selected = item;

        Assert.Equal(section.Name, viewModel.CategoryFilter);
        Assert.Single(viewModel.Items);
        Assert.Equal(menuItem.Category.Name, viewModel.DetailsCategory);
        Assert.Contains(menuItem.Price.Currency, viewModel.DetailsPrice);
        Assert.Contains("Preparation:", viewModel.DetailsMore);
    }

    [Fact]
    public void Discount_command_applies_valid_discount_to_order_total()
    {
        var wiring = new Lab09Wiring();
        var store = wiring.CreateStore();
        var item = wiring.CreateSample(1);
        store.Add(item);

        var viewModel = (CatalogueViewModel)wiring.CreateViewModel(store);
        viewModel.AddToOrderCommand.Execute(item);

        viewModel.DiscountInput = "10";

        Assert.True(viewModel.ApplyDiscountCommand.CanExecute(null));
        viewModel.ApplyDiscountCommand.Execute(null);

        Assert.Equal(9m, viewModel.OrderTotal);
        Assert.Equal(10m, viewModel.AppliedDiscountPercent);

        viewModel.DiscountInput = "101";
        Assert.False(viewModel.ApplyDiscountCommand.CanExecute(null));
        viewModel.DiscountInput = "not a number";
        Assert.False(viewModel.ApplyDiscountCommand.CanExecute(null));
    }

    [Fact]
    public async Task Currency_command_converts_order_total_without_changing_order()
    {
        var wiring = new Lab09Wiring();
        var store = wiring.CreateStore();
        var item = wiring.CreateSample(1);
        store.Add(item);

        var viewModel = new CatalogueViewModel(store, new FakeRates(0.5m));
        viewModel.AddToOrderCommand.Execute(item);

        var command = (AsyncRelayCommand)viewModel.ConvertOrderCommand;
        command.Execute(null);
        await WaitForCommandAsync(command);

        Assert.Equal("5.00 EUR", viewModel.ConvertedOrderDisplay);
        Assert.Equal(10m, viewModel.OrderTotal);
    }

    [Fact]
    public async Task Completing_order_saves_history_and_starts_a_fresh_order()
    {
        var wiring = new Lab09Wiring();
        var store = wiring.CreateStore();
        var item = wiring.CreateSample(1);
        store.Add(item);
        var folder = Path.Combine(Path.GetTempPath(), "cozycup-" + Guid.NewGuid().ToString("N"));
        var historyPath = Path.Combine(folder, "completed-orders.json");
        var reportsFolder = Path.Combine(folder, "reports");

        try
        {
            var viewModel = new CatalogueViewModel(
                store,
                new FakeRates(1m),
                historyPath,
                reportsFolder,
                Path.Combine(folder, "inventory.json"));
            viewModel.AddToOrderCommand.Execute(item);

            var command = (AsyncRelayCommand)viewModel.CompleteOrderCommand;
            command.Execute(null);
            await WaitForCommandAsync(command);

            var history = await CompletedOrderHistoryFile.LoadAsync(
                historyPath,
                TestContext.Current.CancellationToken);
            Assert.Single(history.Orders);
            Assert.Equal(10m, Assert.Single(history.Orders).Total);
            Assert.Empty(viewModel.OrderLines);
            Assert.Equal(0m, viewModel.OrderTotal);
            Assert.Contains("saved and exported", viewModel.OperationMessage);
            var reportPath = Path.Combine(
                reportsFolder,
                $"end-of-day-{DateOnly.FromDateTime(DateTime.Now):yyyy-MM-dd}.txt");
            var report = await File.ReadAllTextAsync(
                reportPath,
                TestContext.Current.CancellationToken);
            Assert.Contains(item.DisplayName, report);
            Assert.Contains("Completed orders: 1", report);

            Assert.True(viewModel.CompleteOrderCommand.CanExecute(null));
            command.Execute(null);
            await WaitForCommandAsync(command);
            history = await CompletedOrderHistoryFile.LoadAsync(
                historyPath,
                TestContext.Current.CancellationToken);
            Assert.Single(history.Orders);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Checkout_records_takeaway_payment_and_vat_on_receipt()
    {
        var wiring = new Lab09Wiring();
        var store = wiring.CreateStore();
        var item = wiring.CreateSample(1);
        store.Add(item);
        var folder = Path.Combine(Path.GetTempPath(), "cozycup-" + Guid.NewGuid().ToString("N"));
        var historyPath = Path.Combine(folder, "completed-orders.json");

        try
        {
            var viewModel = new CatalogueViewModel(
                store,
                new FakeRates(0.5m),
                historyPath,
                Path.Combine(folder, "reports"),
                Path.Combine(folder, "inventory.json"));
            viewModel.SelectedServiceMode = "Takeaway";
            viewModel.SelectedPaymentMethod = "Card";
            viewModel.PaymentCurrency = "EUR";
            viewModel.AddToOrderCommand.Execute(item);

            Assert.Single(viewModel.RecentlyUsedItems);
            Assert.Contains("Service: Takeaway", viewModel.ReceiptPreview);
            Assert.Contains("Payment: Card", viewModel.ReceiptPreview);
            Assert.Contains("VAT included:", viewModel.ReceiptPreview);
            Assert.Contains("Payment currency: EUR", viewModel.ReceiptPreview);

            var command = (AsyncRelayCommand)viewModel.CheckoutCommand;
            command.Execute(null);
            await WaitForCommandAsync(command);

            var history = await CompletedOrderHistoryFile.LoadAsync(
                historyPath,
                TestContext.Current.CancellationToken);
            var paidOrder = Assert.Single(history.Orders);
            Assert.Equal(ServiceMode.Takeaway, paidOrder.ServiceMode);
            Assert.Equal(PaymentMethod.Card, paidOrder.PaymentMethod);
            Assert.Empty(viewModel.OrderLines);
            Assert.Contains("Demo payment approved", viewModel.OperationMessage);
            Assert.Contains("5.00 EUR", viewModel.OperationMessage);
            Assert.Contains("Paid: 5.00 EUR", viewModel.ReceiptPreview);
            Assert.Contains("VAT included:", viewModel.ReceiptPreview);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Inventory_setup_warns_when_stock_is_low_and_resets_at_new_day()
    {
        var store = new Lab09Wiring().CreateStore();
        var item = new Food(
            "stock-item",
            "Blueberry Muffin",
            new Category("pastry", "Pastries"),
            new Money(8m, "RON"));
        store.Add(item);
        var folder = Path.Combine(Path.GetTempPath(), "cozycup-" + Guid.NewGuid().ToString("N"));
        var inventoryPath = Path.Combine(folder, "inventory.json");

        try
        {
            var viewModel = new CatalogueViewModel(
                store,
                new FakeRates(1m),
                Path.Combine(folder, "orders.json"),
                Path.Combine(folder, "reports"),
                inventoryPath);
            Assert.True(viewModel.IsHomePage);
            viewModel.ShowInventoryCommand.Execute(null);
            Assert.True(viewModel.IsInventoryPage);

            var row = Assert.Single(viewModel.InventoryItems, entry => entry.Item.Id == item.Id);
            row.StartingQuantityInput = "1";
            var saveCommand = (AsyncRelayCommand)viewModel.SaveStartingStockCommand;
            saveCommand.Execute(row);
            await WaitForCommandAsync(saveCommand);
            Assert.Equal(1, row.RemainingQuantity);

            viewModel.AddToOrderCommand.Execute(item);
            Assert.Equal(0, row.RemainingQuantity);
            Assert.Equal("SOLD OUT", row.StockStatus);
            viewModel.AddItemToOrder(item);
            Assert.Contains("sold out", viewModel.OperationMessage, StringComparison.OrdinalIgnoreCase);

            var savedLevels = new Dictionary<string, InventoryStockLevel>
            {
                [item.Id] = new(1, 0)
            };
            await InventoryFile.SaveAsync(
                inventoryPath,
                DateOnly.FromDateTime(DateTime.Today),
                savedLevels,
                TestContext.Current.CancellationToken);
            var tomorrow = InventoryFile.Load(
                inventoryPath,
                DateOnly.FromDateTime(DateTime.Today).AddDays(1),
                [item]);
            Assert.Equal(new InventoryStockLevel(1, 1), tomorrow[item.Id]);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Breakfast_combo_unavailability_explains_the_hours()
    {
        var store = new Lab09Wiring().CreateStore();
        var part = new Food(
            "breakfast-part",
            "Croissant",
            new Category("pastry", "Pastries"),
            new Money(8m, "RON"));
        var combo = new Combo(
            "breakfast-window",
            "Breakfast Combo",
            new Category("combo", "Combos"),
            new Money(15m, "RON"),
            [part],
            availableFrom: new TimeOnly(7, 0),
            availableUntil: new TimeOnly(11, 0));
        store.Add(combo);
        var viewModel = new CatalogueViewModel(store, new FakeRates(1m));
        var todayAtNoon = DateTime.Today.AddHours(12);

        viewModel.UpdateClock(todayAtNoon);
        viewModel.Selected = combo;
        viewModel.AddItemToOrder(combo);

        Assert.Equal("Not available now · available 07:00–11:00", viewModel.DetailsAvailability);
        Assert.Contains("not available now", viewModel.OperationMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("07:00–11:00", viewModel.OperationMessage);
    }

    [Fact]
    public void Lunch_combo_has_its_own_selling_window()
    {
        var store = new Lab09Wiring().CreateStore();
        var lunch = new Combo(
            "lunch-window",
            "Lunch Combo",
            new Category("combo", "Combos"),
            new Money(25m, "RON"),
            [
                new Food(
                    "lunch-part",
                    "Sandwich",
                    new Category("sandwich", "Sandwiches"),
                    new Money(20m, "RON"))
            ],
            availableFrom: new TimeOnly(11, 0),
            availableUntil: new TimeOnly(15, 0));
        store.Add(lunch);
        var viewModel = new CatalogueViewModel(store, new FakeRates(1m));

        viewModel.UpdateClock(DateTime.Today.AddHours(10));
        viewModel.Selected = lunch;
        viewModel.AddItemToOrder(lunch);

        Assert.Equal("Not available now · available 11:00–15:00", viewModel.DetailsAvailability);
        Assert.Contains("11:00–15:00", viewModel.OperationMessage);

        viewModel.UpdateClock(DateTime.Today.AddHours(12));
        Assert.Equal("Available now · 11:00–15:00", viewModel.DetailsAvailability);
    }

    private static async Task WaitForCommandAsync(AsyncRelayCommand command)
    {
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (command.IsRunning && DateTime.UtcNow < timeout)
            await Task.Delay(10);

        Assert.False(command.IsRunning, "The command did not finish in time.");
    }
}