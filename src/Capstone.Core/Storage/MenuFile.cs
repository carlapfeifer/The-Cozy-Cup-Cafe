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
    public const int CurrentFormatVersion = 1;
    
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    internal static JsonSerializerOptions SerializerOptions => Options;
    private sealed class MenuDocument
    {
        public int FormatVersion { get; set; }
        public DateTime SavedAtUtc { get; set; }
        public List<ItemRow>? Items { get; set; }
    }

    // One row of the file: a field for everything an item carries, and the kind.
    internal sealed class ItemRow
    {
        public ItemRow(){}
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
        public bool? IsFeatured { get; set; }
        public string? Sku { get; set; }
        public bool? IsHot { get; set; }
        public bool? IsDecaf { get; set; }
        public int? PreparationTimeMinutes { get; set; }
        public string? AvailableFrom { get; set; }
        public string? AvailableUntil { get; set; }
        public List<string>? Ingredients { get; set; }
        public string? Origin { get; set; }
        
        public decimal? CategoryVatPercent { get; set; }
        public string? CategoryIcon { get; set; }
        public string? CategoryDisplayColor { get; set; }
        public decimal? DineInVatPercent { get; set; }
        public decimal? TakeawayVatPercent { get; set; }
        public List<ItemRow>? Parts { get; set; }      // combos only
    }

public static Task SaveAsync(IItemStore store, string path) =>
    SaveAsync(store, path, CancellationToken.None);

public static async Task SaveAsync(
    IItemStore store,
    string path,
    CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(store);
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    cancellationToken.ThrowIfCancellationRequested();

    var document = new MenuDocument
    {
        FormatVersion = CurrentFormatVersion,
        SavedAtUtc = DateTime.UtcNow,
        Items = store.All().Select(ToRow).ToList()
    };

    await AtomicFile.WriteAsync(
        path,
        async (stream, token) =>
            await JsonSerializer.SerializeAsync(stream, document, Options, token),
        cancellationToken);
}

public static Task<IItemStore> LoadAsync(string path) =>
    LoadAsync(path, CancellationToken.None);

public static async Task<IItemStore> LoadAsync(
    string path,
    CancellationToken cancellationToken)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    cancellationToken.ThrowIfCancellationRequested();

    FileStream stream;

    try
    {
        stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous);
    }
    catch (FileNotFoundException)
    {
        return new CafeStore();
    }
    catch (DirectoryNotFoundException)
    {
        return new CafeStore();
    }

    try
    {
        await using (stream)
        using (var document = await JsonDocument.ParseAsync(
                   stream,
                   cancellationToken: cancellationToken))
        {
            List<ItemRow> rows;

            // Accept the old array format so existing saved files still load.
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                rows = document.RootElement.Deserialize<List<ItemRow>>(Options)
                       ?? throw new InvalidDataException("The menu file has no item list.");
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var saved = document.RootElement.Deserialize<MenuDocument>(Options)
                            ?? throw new InvalidDataException("The menu file is invalid.");

                if (saved.FormatVersion != CurrentFormatVersion)
                    throw new InvalidDataException(
                        $"Unsupported menu format version {saved.FormatVersion}.");

                rows = saved.Items
                       ?? throw new InvalidDataException("The menu file has no item list.");
            }
            else
            {
                throw new InvalidDataException("The menu file must contain an object or item array.");
            }

            var store = new CafeStore();
            foreach (var row in rows)
                store.Add(FromRow(row));

            return store;
        }
    }
    catch (JsonException ex)
    {
        throw new InvalidDataException($"'{path}' is not a valid menu file.", ex);
    }
    catch (ArgumentException ex)
    {
        throw new InvalidDataException($"'{path}' contains an invalid menu item: {ex.Message}", ex);
    }
}

   

    internal static ItemRow ToRow(IItem item)
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
            IsFeatured = menuItem.IsFeatured,
            Sku = menuItem.Sku,
            PreparationTimeMinutes = menuItem.PreparationTimeMinutes,
            CategoryVatPercent = menuItem.Category.VatPercent,
            CategoryIcon = menuItem.Category.Icon,
            CategoryDisplayColor = menuItem.Category.DisplayColor,
            DineInVatPercent = menuItem.Vat.DineInPercent,
            TakeawayVatPercent = menuItem.Vat.TakeawayPercent,
            AvailableFrom = menuItem.AvailableFrom?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            AvailableUntil = menuItem.AvailableUntil?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            Ingredients = menuItem.Ingredients.Count > 0 ? menuItem.Ingredients.ToList() : null,
            Origin = menuItem.Origin.ToString()
        };

        switch (menuItem)
        {
            case Drink drink:
                row.Kind = "drink";
                row.Size = drink.Size.ToString();
                row.IsHot = drink.IsHot;
                row.IsDecaf = drink.IsDecaf;
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

    internal static MenuItem FromRow(ItemRow? row)
    {
        if (row?.Id is null || row.DisplayName is null || row.CategoryId is null || row.CategoryName is null
            || row.Amount is null || row.Currency is null || row.Kind is null )
            throw new InvalidDataException("A row is missing a required field.");
        if (row.Sku is not null &&
            string.IsNullOrWhiteSpace(row.Sku))
        {
            throw new InvalidDataException("A row contains a blank SKU.");
        }

        var category = new Category(
            row.CategoryId,
            row.CategoryName,
            row.CategoryVatPercent ?? 11m,
            row.CategoryIcon ?? "category",
            row.CategoryDisplayColor ?? "#D8B59D");

        var price = new Money(row.Amount.Value, row.Currency);
        var vat = new VatInfo(
            row.DineInVatPercent ?? 11m,
            row.TakeawayVatPercent ?? 11m);
        var available = row.IsAvailable ?? true;
        var featured = row.IsFeatured ?? false;
        var sku = row.Sku;
        var preparationTimeMinutes = row.PreparationTimeMinutes ?? 5;
        var origin = Enum.TryParse<ItemOrigin>(row.Origin, ignoreCase: true, out var parsedOrigin)
            ? parsedOrigin
            : ItemOrigin.MadeInHouse;
        if (!TryParseAvailabilityWindow(row.AvailableFrom, row.AvailableUntil, out var availableFrom, out var availableUntil))
            throw new InvalidDataException("A row has an invalid availability window.");

        switch (row.Kind)
        {
            case "drink":
                if (row.Size is null
                    || !Enum.TryParse<Size>(row.Size, ignoreCase: true, out var size)
                    || !Enum.IsDefined(size))
                {
                    throw new InvalidDataException($"'{row.Size}' is not a drink size.");
                }
                return new Drink(
                    row.Id,
                    row.DisplayName,
                    category,
                    price,
                    size,
                    ParseAllergens(row.Allergens),
                    available,
                    featured,
                    row.IsHot ?? true,
                    preparationTimeMinutes,
                    sku,
                    vat,
                    row.IsDecaf ?? false,
                    availableFrom,
                    availableUntil,
                    row.Ingredients,
                    origin
                    );

            case "food":
                return new Food(
                    row.Id,
                    row.DisplayName,
                    category,
                    price,
                    ParseAllergens(row.Allergens),
                    available,
                    featured,
                    preparationTimeMinutes,
                    sku,vat,
                    availableFrom,
                    availableUntil,
                    row.Ingredients,
                    origin);

            case "combo":
                if (row.Parts is not { Count: > 0 })
                    throw new InvalidDataException("A combo must contain at least one part.");

                var parts = row.Parts.Select(FromRow).ToList();
               
                return new Combo(
                    row.Id,
                    row.DisplayName,
                    category,
                    price,
                    parts,
                    available,
                    featured,
                    preparationTimeMinutes,
                    sku,vat,
                    availableFrom,
                    availableUntil,
                    row.Ingredients,
                    origin);

            default:
                throw new InvalidDataException($"'{row.Kind}' is not a kind of menu item.");
        }
    }

    internal static List<string> AllergenNames(Allergen allergens) =>
        Enum.GetValues<Allergen>()
            .Where(a => a != Allergen.None && allergens.HasFlag(a))
            .Select(a => a.ToString())
            .ToList();

    private static bool TryParseAvailabilityWindow(
        string? from,
        string? until,
        out TimeOnly? availableFrom,
        out TimeOnly? availableUntil)
    {
        availableFrom = null;
        availableUntil = null;
        if (from is null && until is null)
            return true;

        if (from is null
            || until is null
            || !TimeOnly.TryParse(from, System.Globalization.CultureInfo.InvariantCulture, out var parsedFrom)
            || !TimeOnly.TryParse(until, System.Globalization.CultureInfo.InvariantCulture, out var parsedUntil)
            || parsedFrom == parsedUntil)
        {
            return false;
        }

        availableFrom = parsedFrom;
        availableUntil = parsedUntil;
        return true;
    }

    internal static Allergen ParseAllergens(List<string>? names)
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