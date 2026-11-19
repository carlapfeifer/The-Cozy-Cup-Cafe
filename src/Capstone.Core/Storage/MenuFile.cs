using System.Text.Json;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Storage;

/// <summary>
/// Saves the menu to a JSON file and loads it back.
/// The file holds ROWS (one per item, with every field and the kind), never the store object itself:
/// serializing the store would write only the declared element type (Id and DisplayName) and lose the rest.
/// </summary>
public static class MenuFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    // One row of the file: a field for everything an item carries, and the kind.
    private sealed class ItemRow
    {
        public string? Kind { get; set; }              // "drink", "food" or "combo"
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? Size { get; set; }              // drinks only
        public List<string>? Allergens { get; set; }   // not stored for combos: they come from the parts
        public bool? IsAvailable { get; set; }
        public List<ItemRow>? Parts { get; set; }      // combos only
    }

    public static async Task SaveAsync(IItemStore store, string path)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } folder)
            Directory.CreateDirectory(folder);          // asking the user to mkdir first is rude

        var rows = store.All().Select(ToRow).ToList();  // our rows, not the store object

        // Write the whole file next to the real one, then move it over the real one.
        // A crash halfway through leaves the old file intact instead of half a new one.
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, rows, Options);
        }                                               // closed here, before the move
        File.Move(tempPath, path, overwrite: true);
    
    }

    public static async Task<IItemStore> LoadAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Nothing saved yet (or no such folder): the first run of every program. Not an error.
        if (!File.Exists(path))
            return new CafeStore();

        try
        {
            await using var stream = File.OpenRead(path);
            var rows = await JsonSerializer.DeserializeAsync<List<ItemRow>>(stream, Options)
                       ?? throw new InvalidDataException($"'{path}' does not contain a list of menu items.");

            var store = new CafeStore();
            foreach (var row in rows)
                store.Add(FromRow(row));                // a duplicate id in the file throws ArgumentException
            return store;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"'{path}' is not a valid menu file.", ex);
        }
        catch (ArgumentException ex)                    // bad values, rejected by the model's own guards
        {
            throw new InvalidDataException($"'{path}' holds an item that is not valid: {ex.Message}", ex);
        }
    }

    private static ItemRow ToRow(IItem item)
    {
        if (item is not MenuItem menuItem)
            throw new ArgumentException($"Only {nameof(MenuItem)} can be saved, got {item.GetType().Name}.", nameof(item));

        var row = new ItemRow
        {
            Id = menuItem.Id,
            DisplayName = menuItem.DisplayName,
            CategoryId = menuItem.Category.Id,
            CategoryName = menuItem.Category.Name,
            Amount = menuItem.Price.Amount,
            Currency = menuItem.Price.Currency,
            IsAvailable = menuItem.IsAvailable,
        };

        switch (menuItem)
        {
            case Drink drink:
                row.Kind = "drink";
                row.Size = drink.Size.ToString();
                row.Allergens = AllergenNames(drink.Allergens);
                break;
            case Food food:
                row.Kind = "food";
                row.Allergens = AllergenNames(food.Allergens);
                break;
            case Combo combo:
                row.Kind = "combo";
                row.Parts = combo.Parts.Select(ToRow).ToList();
                break;
            default:
                throw new ArgumentException($"Unknown kind {menuItem.GetType().Name}.", nameof(item));
        }

        return row;
    }

    private static MenuItem FromRow(ItemRow? row)
    {
        if (row?.Id is null || row.DisplayName is null || row.CategoryId is null || row.CategoryName is null
            || row.Amount is null || row.Currency is null)
            throw new InvalidDataException("A row is missing a required field.");

        var category = new Category(row.CategoryId, row.CategoryName);
        var price = new Money(row.Amount.Value, row.Currency);
        var available = row.IsAvailable ?? true;

        switch (row.Kind)
        {
            case "drink":
                if (!Enum.TryParse<Size>(row.Size, ignoreCase: true, out var size) || !Enum.IsDefined(size))
                    throw new InvalidDataException($"'{row.Size}' is not a drink size.");
                return new Drink(row.Id, row.DisplayName, category, price, size, ParseAllergens(row.Allergens), available);

            case "food":
                return new Food(row.Id, row.DisplayName, category, price, ParseAllergens(row.Allergens), available);

            case "combo":
                var parts = (row.Parts ?? new List<ItemRow>()).Select(FromRow).ToList();
                return new Combo(row.Id, row.DisplayName, category, price, parts, available);

            default:
                throw new InvalidDataException($"'{row.Kind}' is not a kind of menu item.");
        }
    }

    private static List<string> AllergenNames(Allergen allergens) =>
        Enum.GetValues<Allergen>()
            .Where(a => a != Allergen.None && allergens.HasFlag(a))
            .Select(a => a.ToString())
            .ToList();

    private static Allergen ParseAllergens(List<string>? names)
    {
        var result = Allergen.None;
        foreach (var name in names ?? new List<string>())
        {
            if (!Enum.TryParse<Allergen>(name, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                throw new InvalidDataException($"'{name}' is not an allergen.");
            result |= parsed;
        }
        return result;
    }
}