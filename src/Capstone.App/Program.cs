// ───────────────────────────────────────────────────────────────────────────────
//  The Cozy Cup · café menu demo
//
//  Keep this file short. Real logic belongs in Capstone.Core, where the tests can reach it.
//
//  Sections, in the order they run:
//    Lab 02 · Model & mapper        starter menu from JSON
//    Lab 08 · Persistence (load)    saved menu from the previous run
//    Lab 06 · Subscribers           attach listeners to the store
//    Lab 05 · The store             add items, iterate
//    Lab 06 · Changes               update / availability / price / remove
//    Lab 04 · Polymorphism          every item describes itself
//    Lab 06 · What listeners saw    tallies and the recent log
//    Lab 06 · Functions as values   closure and a function that builds a function
//    Lab 07 · LINQ queries          names, sorted, pages, longest, summary
//    Lab 07 · Café reports          five questions over a day of orders
//    Lab 08 · Persistence (save)    save, then reload with a fresh wiring instance
// ───────────────────────────────────────────────────────────────────────────────

using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Subscribers;
using Capstone.Core.Wiring;
using System.Text.Json;
using SharpQuest.Contracts;
using Capstone.Core.Storage;
using Capstone.Core.Currency;
using Capstone.Core.Payments;

// ── Setup ──────────────────────────────────────────────────────────────────────
var coffee = new Category("cof", "Coffee");
var pastry = new Category("pas", "Pastries");
var mapper = new MenuItemMapper();
var menuPath = Path.Combine(AppContext.BaseDirectory, "menu.json");
var persistencePath = Path.Combine(AppContext.BaseDirectory, "menu-state.json");
var orderHistoryPath = Path.Combine(AppContext.BaseDirectory, "completed-orders.json");
var lab08 = new Lab08Wiring();
var store = new CafeStore();


// ══ Lab 02 · Model & mapper ════════════════════════════════════════════════════
// ══ Lab 08 · Persistence (load) ════════════════════════════════════════════════
// After the first run, start from the saved menu; otherwise use the Lab 02 starter menu.
Section("Lab 02 + 08 · Starting menu");

var hasSavedMenu = File.Exists(persistencePath);
var savedMenu = await lab08.LoadAsync(persistencePath);
var menuItems = hasSavedMenu
    ? savedMenu.All().ToList()
    : LoadMenuItems(menuPath, mapper);

Console.WriteLine(hasSavedMenu
    ? $"Loaded {menuItems.Count} menu items from saved state:"
    : $"No saved state found; loaded {menuItems.Count} starter menu items:");


// ══ Lab 06 · Subscribers ═══════════════════════════════════════════════════════
// Each listener attaches itself to the store; the store knows none of them.
var board = new MenuBoard();
var stopBoard = board.Attach(store);

var categories = new CategoryBoard();
categories.Attach(store);

var todayTakings = new TodayTakings();
using var takingsSubscription = todayTakings.Attach(store);

var salesLog = new SalesLog();
using var salesLogSubscription = salesLog.Attach(store);

var availabilityLog = new AvailabilityTransitionLog();
using var availabilitySubscription = availabilityLog.Attach(store);

var featuredMenuCache = new FeaturedMenuCache();
using var featuredCacheSubscription = featuredMenuCache.Attach(store);

var categorySales = new CategorySalesCounter();
using var categorySalesSubscription = categorySales.Attach(store);

var receiptMemory = new ReceiptMemory();
using var receiptSubscription = receiptMemory.Attach(store);

var log = new RecentLog(3);
log.Attach(store);

ConsoleLog.Attach(store);


// ══ Lab 05 · The store (collection) ════════════════════════════════════════════
// Adding items triggers the store's events, so the subscribers react.
Section("Lab 05 · Filling the store");
var itemAddedSubscription =
    store.SubscribeItemAdded(item => Console.WriteLine($"Disposable subscriber saw: {item.DisplayName}"));

foreach (var item in menuItems.Take(3))
    store.Add(item);

itemAddedSubscription.Dispose();
itemAddedSubscription.Dispose(); // Repeated disposal is safe.

stopBoard(); // the board stops listening; the log keeps going

foreach (var item in menuItems.Skip(3))
    store.Add(item);


// ══ Lab 06 · Changes ═══════════════════════════════════════════════════════════
Section("Lab 06 · Changing the menu");

var observedMenuChanges = new List<string>();
using var menuChangeSubscription = store.SubscribeMenuChanged((_, change) => observedMenuChanges.Add(
    $"{change.Change}: {change.Item.DisplayName}"));

var featuredBeforeChanges = featuredMenuCache.GetFeaturedItems(store);
Console.WriteLine(
    $"Featured before changes: {string.Join(", ", featuredBeforeChanges.Select(item => item.DisplayName))}");

store.Update(new Food("pas-001", "Butter Croissant", pastry, new Money(8m, "RON"), Allergen.Gluten));
store.SetAvailability("cof-001", false);
store.SetPrice("pas-002", new Money(12m, "RON"));
store.Remove("cof-002");

var featuredAfterChange = featuredMenuCache.GetFeaturedItems(store);
Console.WriteLine(
    $"Featured menu after changes: {string.Join(", ", featuredAfterChange.Select(item => item.DisplayName))}");

// Demonstrate sold-out and restocked events using a drink already in the menu.
var demoDrink = new Drink(
    "lab06-demo-featured",
    "Demo Featured Latte",
    coffee,
    new Money(12m, "RON"),
    isFeatured: true);

store.Add(demoDrink);

Console.WriteLine(
    $"Featured after demo item added: " +
    $"{string.Join(", ", featuredMenuCache.GetFeaturedItems(store).Select(item => item.DisplayName))}");

store.Update(new Drink(
    "lab06-demo-featured",
    "Demo Featured Latte",
    coffee,
    new Money(12m, "RON"),
    isFeatured: false));

Console.WriteLine(
    $"Featured after demo item update: " +
    $"{string.Join(", ", featuredMenuCache.GetFeaturedItems(store).Select(item => item.DisplayName))}");

store.SetAvailability("lab06-demo-featured", false);
store.SetAvailability("lab06-demo-featured", true);

Console.WriteLine(
    $"Sold-out items logged: {string.Join(", ", salesLog.SoldOutItems.Select(item => item.DisplayName))}");
Console.WriteLine(
    $"Restocked items logged: {string.Join(", ", availabilityLog.RestockedItems.Select(item => item.DisplayName))}");

store.Remove("lab06-demo-featured");

Console.WriteLine("Menu changes observed:");
foreach (var change in observedMenuChanges)
    Console.WriteLine($"  {change}");


// ══ Lab 04 · Polymorphism ══════════════════════════════════════════════════════
// Each item describes itself: no type test, the override decides.
Section("Lab 04 · The menu, as each item describes itself");

foreach (var menuItem in store.OfType<MenuItem>())
    Console.WriteLine($"  {menuItem.Describe()}");


// ══ Lab 06 · What the listeners saw ════════════════════════════════════════════
Section("Lab 06 · What the subscribers observed");

Console.WriteLine("Menu board tally (stopped before the last adds):");
foreach (var (kind, count) in board.Tally)
    Console.WriteLine($"  {kind}: {count}");

Console.WriteLine("Category tally:");
foreach (var (category, count) in categories.Tally)
    Console.WriteLine($"  {category}: {count}");

Console.WriteLine("Recently added:");
foreach (var item in log.Items())
    Console.WriteLine($"  {item.DisplayName}");


// ══ Lab 06 · Functions as values ═══════════════════════════════════════════════
Section("Lab 06 · Functions as values");

// A closure: the lambda keeps a live link to menuValue, not a copy.
var menuValue = 0m;
StoreFunctions.ForEach(store, item =>
{
    if (item is MenuItem menuItem && menuItem.IsAvailable)
        menuValue += menuItem.Price.Amount;
});
Console.WriteLine($"Value of everything on sale: {menuValue:0.00} RON");

// A function that builds a function: AtMostPrice(10m) returns a rule that remembers 10.
var cheap = StoreFunctions.Where(store, MenuFilters.AtMostPrice(10m));
Console.WriteLine($"Items up to 10 RON: {string.Join(", ", cheap.Select(item => item.DisplayName))}");

var menuDescriptions = StoreFunctions.Map(
    store,
    item => item is MenuItem menuItem
        ? menuItem.Describe()
        : item.DisplayName);

Console.WriteLine("Mapped menu descriptions:");
foreach (var description in menuDescriptions)
    Console.WriteLine($"  {description}");


// ══ Lab 07 · LINQ queries ══════════════════════════════════════════════════════
Section("Lab 07 · LINQ queries over the menu");

var lab07 = new Lab07Wiring();

Console.WriteLine("Names:");
Console.WriteLine($"  {string.Join(", ", lab07.Names(store))}");

Console.WriteLine("Sorted menu:");
foreach (var item in lab07.Sorted(store))
    Console.WriteLine($"  {item.DisplayName}");

Console.WriteLine("First page:");
foreach (var item in lab07.Page(store, skip: 0, take: 2))
    Console.WriteLine($"  {item.DisplayName}");

Console.WriteLine("Longest names:");
foreach (var item in lab07.Longest(store, 2))
    Console.WriteLine($"  {item.DisplayName}");

Console.WriteLine($"Summary: {lab07.Summary(store)}");


// ══ Lab 07 · Café reports ══════════════════════════════════════════════════════
// Five questions over a small day of orders (its own menu, independent of the store above).
Section("Lab 07 · Café reports");

var teaCategory = new Category("tea", "Tea");
var sandwichCategory = new Category("san", "Sandwiches");
var coldCategory = new Category("cold", "Cold Drinks");
var saladCategory = new Category("sal", "Salads");

var latte = new Drink("r-1", "Latte", coffee, new Money(14m, "RON"),
    allergens: Allergen.Milk);
var espresso = new Drink("r-2", "Espresso", coffee, new Money(9m, "RON"));
var croissant = new Food("r-3", "Croissant", pastry, new Money(8m, "RON"),
    Allergen.Gluten | Allergen.Milk);
var tart = new Food("r-4", "Almond tart", pastry, new Money(12m, "RON"),
    Allergen.Nuts);
var salad = new Food("r-5", "Garden salad", saladCategory, new Money(16m, "RON"));
var greenTea = new Drink("r-6", "Green Tea", teaCategory, new Money(7m, "RON"));
var veggieSandwich = new Food("r-7", "Veggie Sandwich", sandwichCategory,
    new Money(11m, "RON"));
var icedTea = new Drink("r-8", "Iced Tea", coldCategory, new Money(6m, "RON"),
    isAvailable: false);

var reportMenu = new List<MenuItem>
{
    latte, espresso, croissant, tart, salad, greenTea, veggieSandwich, icedTea
};

var orders = CafeDataGenerator
    .GenerateOrders(reportMenu, 50, seed: 2026)
    .ToList();

Console.WriteLine(
    $"Generated {orders.Count} seeded orders across " +
    $"{reportMenu.Select(item => item.Category.Id).Distinct().Count()} categories.");
Console.WriteLine(
    $"Discounted orders: {orders.Count(order => order.DiscountPercent > 0)}; " +
    $"undiscounted: {orders.Count(order => order.DiscountPercent == 0)}.");
Console.WriteLine(
    $"Unavailable menu items: {string.Join(", ", reportMenu.Where(item => !item.IsAvailable).Select(item => item.DisplayName))}");


Console.WriteLine("Best seller per category:");
foreach (var best in MenuReports.BestSellerPerCategory(orders))
    Console.WriteLine($"  {best.Category}: {best.Item} ({best.UnitsSold} sold)");

Console.WriteLine("Revenue by hour:");
foreach (var hour in MenuReports.RevenueByHour(orders))
    Console.WriteLine($"  {hour.Hour:00}:00  {hour.Revenue:0.00} RON");

Console.WriteLine(
    $"Never ordered: {string.Join(", ", MenuReports.NeverOrdered(reportMenu, orders).Select(i => i.DisplayName))}");
Console.WriteLine($"Average order value: {MenuReports.AverageOrderValue(orders):0.00} RON");
Console.WriteLine(
    $"Vegan under 15 RON: {string.Join(", ", MenuReports.VeganUnder(reportMenu, 15m).Select(i => i.DisplayName))}");

Console.WriteLine("Menu items per category:");
foreach (var row in MenuReports.MenuItemCountPerCategory(reportMenu))
    Console.WriteLine($"  {row.CategoryName}: {row.ItemCount}");

Console.WriteLine("Revenue by payment method:");
foreach (var row in MenuReports.RevenueByPaymentMethod(orders))
    Console.WriteLine($"  {row.Method}: {row.Revenue:0.00} {row.Currency}");

Console.WriteLine("Units sold per item:");
foreach (var row in MenuReports.SalesCountPerMenuItem(orders))
    Console.WriteLine($"  {row.ItemName}: {row.UnitsSold}");

Console.WriteLine("Order lines joined to menu items (first five):");
foreach (var row in MenuReports.JoinOrderLinesToMenu(orders, reportMenu).Take(5))
    Console.WriteLine($"  {row.Quantity}x {row.Item.DisplayName}");

var reportCategories = reportMenu
    .Select(item => item.Category)
    .DistinctBy(category => category.Id)
    .ToList();

Console.WriteLine("Categories joined to menu items:");
foreach (var row in MenuReports.JoinCategoriesToMenu(reportCategories, reportMenu))
    Console.WriteLine($"  {row.Category.Name}: {row.Item.DisplayName}");

var biggestOrder = MenuReports.BiggestOrder(orders);
Console.WriteLine(
    $"Biggest order: {biggestOrder?.Total:0.00} RON, " +
    $"{biggestOrder?.Lines.Sum(line => line.Quantity) ?? 0} items");

var fastestPastry = MenuReports.FastestSellingPastryInFinalTwoHours(orders);
Console.WriteLine(fastestPastry is null
    ? "No pastries sold in the final two hours."
    : $"Fastest-selling pastry, 18:00-20:00: {fastestPastry.Item} ({fastestPastry.UnitsSold} sold)");

Console.WriteLine("Available vegan items:");
foreach (var item in reportMenu.AvailableItems().VeganItems())
    Console.WriteLine($"  {item.ToReceiptLine()}");

Console.WriteLine("Pastries:");
foreach (var item in reportMenu.ByCategory("Pastries"))
    Console.WriteLine($"  {item.ToReceiptLine()}");


using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};
var converter = new CurrencyConverter(new FrankfurterRateProvider(httpClient));

Section("Live menu prices · Frankfurter · EUR");
try
{
    var convertedMenuPrices = await converter.ConvertMenuPricesAsync(
        store.OfType<MenuItem>(),
        "EUR");

    foreach (var (item, price) in convertedMenuPrices)
        Console.WriteLine($"  {item.DisplayName}: {price.Amount:0.00} {price.Currency}");
}
catch (HttpRequestException exception)
{
    Console.WriteLine(
        $"Could not load live EUR menu prices ({exception.Message}). " +
        "The original RON menu prices remain unchanged.");
}
catch (TaskCanceledException exception)
{
    Console.WriteLine(
        $"The live EUR price request timed out ({exception.Message}). " +
        "The original RON menu prices remain unchanged.");
}

var checkoutOrder = orders.First();
var paymentCurrency = "RON"; // Change to "USD" or "RON" to try another currency.

var paymentAmount = await checkoutOrder.ConvertTotalAsync(
    converter,
    paymentCurrency);

var paymentProcessor = new DemoPaymentProcessor();
var completedOrderHistory =
    await CompletedOrderHistoryFile.LoadAsync(orderHistoryPath);

var paymentApproved = await paymentProcessor.ProcessPaymentAsync(paymentAmount);

if (paymentApproved)
{
    completedOrderHistory.Record(checkoutOrder);
    store.NotifyOrderCompleted(checkoutOrder, paymentAmount, DateTime.Now);


    foreach (var (currency, amount) in todayTakings.TotalsByCurrency)
        Console.WriteLine($"Today's takings: {amount:0.00} {currency}");

    foreach (var (categoryId, quantity) in categorySales.UnitsSold)
        Console.WriteLine($"Units sold in {categoryId}: {quantity}");

    foreach (var receipt in receiptMemory.Receipts)
        Console.WriteLine($"Receipt: {receipt}");


    Console.WriteLine($"Completed orders in sales log: {salesLog.CompletedOrders.Count}");

    foreach (var receipt in receiptMemory.Receipts)
    {
        Console.WriteLine($"Receipt: {receipt}");
    }

    Console.WriteLine(
        $"Today's {paymentAmount.Currency} takings: " +
        $"{todayTakings.TotalsByCurrency.GetValueOrDefault(paymentAmount.Currency):0.00}");

    Console.WriteLine(
        $"Orders in sales log: {salesLog.CompletedOrders.Count}");
    Console.WriteLine(
        $"Demo payment approved (no real charge): {paymentAmount.Amount:0.00} {paymentAmount.Currency}");
    Console.WriteLine($"Completed orders: {completedOrderHistory.Count}");
}
else
{
    Console.WriteLine("Payment declined; the order was not recorded as completed.");
}

await CompletedOrderHistoryFile.SaveAsync(
    completedOrderHistory,
    orderHistoryPath);
try
{
    var totalInEur =
        await checkoutOrder.ConvertTotalAsync(converter, "EUR");
    var totalInUsd =
        await checkoutOrder.ConvertTotalAsync(converter, "USD");

    Console.WriteLine($"Payable total: {totalInEur.Amount:0.00} EUR");
    Console.WriteLine($"Payable total: {totalInUsd.Amount:0.00} USD");
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"Live currency conversion failed: {exception.Message}");
}
catch (TaskCanceledException exception)
{
    Console.WriteLine($"Live currency conversion timed out: {exception.Message}");
}

// ══ Lab 08 · Persistence (save) ════════════════════════════════════════════════
// Save the current snapshot, then load it with a fresh wiring instance, as a restart would.
Section("Lab 08 · Save and reload");

await lab08.SaveAsync(store, persistencePath);
Console.WriteLine($"Saved menu state to {persistencePath}");

var reloadedMenu = await new Lab08Wiring().LoadAsync(persistencePath);
Console.WriteLine("Reloaded from disk through a fresh Lab 08 wiring instance:");
foreach (var item in reloadedMenu.All().OfType<MenuItem>())
    Console.WriteLine(
        $"  {item.GetType().Name}: {item.DisplayName} — {item.Price.Amount:0.00} {item.Price.Currency}");
Console.WriteLine("Run the app again to see this saved menu loaded at startup.");

// Stretch: a save writes menu-state.json.tmp first and only then moves it over the real file,
// so a crash halfway through a save leaves the previous file intact.
var tempPath = persistencePath + ".tmp";
await File.WriteAllTextAsync(tempPath, "[{\"kind\":\"drink\",\"id\":\"half-writ"); // what a crashed save leaves behind
var afterCrash = await lab08.LoadAsync(persistencePath);
Console.WriteLine($"After a simulated crash mid-save, the saved menu still loads: {afterCrash.Count} items");
File.Delete(tempPath);

// ───────────────────────────────────────────────────────────────────────────────
//  Helpers
// ───────────────────────────────────────────────────────────────────────────────

// Prints a banner so each lab's output is easy to find.
static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine($"══ {title} ".PadRight(64, '═'));
}

// Lab 02: parse the starter JSON and map each valid row into a menu item.
static List<IItem> LoadMenuItems(string path, IItemMapper mapper)
{
    using var document = JsonDocument.Parse(File.ReadAllText(path));

    if (document.RootElement.ValueKind != JsonValueKind.Array)
        throw new InvalidDataException("The menu JSON must contain an array.");

    var items = new List<IItem>();

    foreach (var element in document.RootElement.EnumerateArray())
    {
        var item = mapper.FromJson(element.GetRawText());

        if (item is null)
        {
            Console.WriteLine($"Skipped invalid menu entry: {element.GetRawText()}");
            continue;
        }

        items.Add(item);
    }

    return items;
}