// Keep this file short. Real logic belongs in Capstone.Core, where the tests can reach it.

using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Subscribers;
using Capstone.Core.Wiring;
using System.Text.Json;
using SharpQuest.Contracts;

var store = new CafeStore();
var coffee = new Category("cof", "Coffee");
var pastry = new Category("pas", "Pastries");
var mapper = new MenuItemMapper();
var menuPath = Path.Combine(AppContext.BaseDirectory, "menu.json");
var menuItems = LoadMenuItems(menuPath, mapper);

Console.WriteLine($"Loaded {menuItems.Count} valid menu items:");
foreach (var item in menuItems)
    Console.WriteLine($"  {item.DisplayName}");

// The store knows none of these. Each one just attaches itself.
var board = new MenuBoard();
var stopBoard = board.Attach(store);

var categories = new CategoryBoard();
categories.Attach(store);

var log = new RecentLog(3);
log.Attach(store);

ConsoleLog.Attach(store);

foreach (var item in menuItems.Take(3))
    store.Add(item);

stopBoard(); // the board stops listening; the log keeps going

foreach (var item in menuItems.Skip(3))
    store.Add(item);

store.Update(new Food("pas-001", "Butter Croissant", pastry, new Money(8m, "RON"), Allergen.Gluten));
store.SetAvailability("cof-001", false);
store.SetPrice("pas-002", new Money(12m, "RON"));
store.Remove("cof-002");

Console.WriteLine("Menu board tally (stopped before the last adds):");
foreach (var (kind, count) in board.Tally)
    Console.WriteLine($"  {kind}: {count}");

Console.WriteLine("Category tally:");
foreach (var (category, count) in categories.Tally)
    Console.WriteLine($"  {category}: {count}");

Console.WriteLine("Recently added:");
foreach (var item in log.Items())
    Console.WriteLine($"  {item.DisplayName}");

Console.WriteLine("Menu:");
foreach (var menuItem in store.OfType<MenuItem>())
    Console.WriteLine($"  {menuItem.Describe()}");

// Step 2, a closure: the lambda keeps a live link to menuValue, not a copy
var menuValue = 0m;
StoreFunctions.ForEach(store, item =>
{
    if (item is MenuItem menuItem && menuItem.IsAvailable)
        menuValue += menuItem.Price.Amount;
});
Console.WriteLine($"Value of everything on sale: {menuValue:0.00} RON");

// A function that builds a function: AtMostPrice(10m) returns a rule that remembers 10
var cheap = StoreFunctions.Where(store, MenuFilters.AtMostPrice(10m));
Console.WriteLine($"Items up to 10 RON: {string.Join(", ", cheap.Select(item => item.DisplayName))}");

// lab 7

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