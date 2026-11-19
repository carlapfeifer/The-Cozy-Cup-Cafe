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

// ── Setup ──────────────────────────────────────────────────────────────────────
var coffee = new Category("cof", "Coffee");
var pastry = new Category("pas", "Pastries");
var mapper = new MenuItemMapper();
var menuPath = Path.Combine(AppContext.BaseDirectory, "menu.json");
var persistencePath = Path.Combine(AppContext.BaseDirectory, "menu-state.json");
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
foreach (var item in menuItems)
    Console.WriteLine($"  {item.DisplayName}");


// ══ Lab 06 · Subscribers ═══════════════════════════════════════════════════════
// Each listener attaches itself to the store; the store knows none of them.
var board = new MenuBoard();
var stopBoard = board.Attach(store);

var categories = new CategoryBoard();
categories.Attach(store);

var log = new RecentLog(3);
log.Attach(store);

ConsoleLog.Attach(store);


// ══ Lab 05 · The store (collection) ════════════════════════════════════════════
// Adding items triggers the store's events, so the subscribers react.
Section("Lab 05 · Filling the store");

foreach (var item in menuItems.Take(3))
    store.Add(item);

stopBoard(); // the board stops listening; the log keeps going

foreach (var item in menuItems.Skip(3))
    store.Add(item);


// ══ Lab 06 · Changes ═══════════════════════════════════════════════════════════
Section("Lab 06 · Changing the menu");

store.Update(new Food("pas-001", "Butter Croissant", pastry, new Money(8m, "RON"), Allergen.Gluten));
store.SetAvailability("cof-001", false);
store.SetPrice("pas-002", new Money(12m, "RON"));
store.Remove("cof-002");


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

var latte = new Drink("r-1", "Latte", coffee, new Money(14m, "RON"), Size.Medium, Allergen.Milk);
var espresso = new Drink("r-2", "Espresso", coffee, new Money(9m, "RON"));
var croissant = new Food("r-3", "Croissant", pastry, new Money(8m, "RON"), Allergen.Gluten | Allergen.Milk);
var tart = new Food("r-4", "Almond tart", pastry, new Money(12m, "RON"), Allergen.Nuts);
var salad = new Food("r-5", "Garden salad", new Category("sal", "Salads"), new Money(16m, "RON"));
var reportMenu = new List<MenuItem> { latte, espresso, croissant, tart, salad };

var morning = new Order("RON", new DateTime(2026, 11, 19, 9, 10, 0));
morning.Add(latte, 2);
morning.Add(croissant, 1);
var lunch = new Order("RON", new DateTime(2026, 11, 19, 12, 30, 0));
lunch.Add(espresso, 1);
lunch.Add(tart, 1);
lunch.ApplyDiscount(10m);
var orders = new List<Order> { morning, lunch };

Console.WriteLine("Best seller per category:");
foreach (var best in MenuReports.BestSellerPerCategory(orders))
    Console.WriteLine($"  {best.Category}: {best.Item} ({best.UnitsSold} sold)");

Console.WriteLine("Revenue by hour:");
foreach (var hour in MenuReports.RevenueByHour(orders))
    Console.WriteLine($"  {hour.Hour:00}:00  {hour.Revenue:0.00} RON");

Console.WriteLine($"Never ordered: {string.Join(", ", MenuReports.NeverOrdered(reportMenu, orders).Select(i => i.DisplayName))}");
Console.WriteLine($"Average order value: {MenuReports.AverageOrderValue(orders):0.00} RON");
Console.WriteLine($"Vegan under 15 RON: {string.Join(", ", MenuReports.VeganUnder(reportMenu, 15m).Select(i => i.DisplayName))}");


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
await File.WriteAllTextAsync(tempPath, "[{\"kind\":\"drink\",\"id\":\"half-writ");   // what a crashed save leaves behind
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