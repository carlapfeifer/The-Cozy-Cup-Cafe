// Keep this file short. Real logic belongs in Capstone.Core, where the tests can reach it.

using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Subscribers;

var store = new CafeStore();
var coffee = new Category("cof", "Coffee");
var pastry = new Category("pas", "Pastries");

// The store knows none of these. Each one just attaches itself.
var board = new MenuBoard();
var stopBoard = board.Attach(store);

var categories = new CategoryBoard();
categories.Attach(store);

var log = new RecentLog(3);
log.Attach(store);

ConsoleLog.Attach(store);

store.Add(new Drink("cof-001", "Latte", coffee, new Money(14m, "RON"), Size.Large));
store.Add(new Drink("cof-002", "Espresso", coffee, new Money(9m, "RON"), Size.Small));
store.Add(new Food("pas-001", "Croissant", pastry, new Money(8m, "RON")));
stopBoard(); // the board stops listening; the log keeps going
store.Add(new Food("pas-002", "Pistachio roll", pastry, new Money(11m, "RON")));
store.Add(new Drink("cof-003", "Mocha", coffee, new Money(13m, "RON"), isAvailable: false)); // sold out
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