using System.Text.Json;
using Capstone.Core.Models;

namespace Capstone.Core.Storage;

public static class InventoryFile
{
    private const int DefaultStartingQuantity = 20;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private sealed class InventoryDocument
    {
        public DateOnly BusinessDate { get; set; }
        public Dictionary<string, InventoryStockLevel>? Items { get; set; }
    }

    public static Dictionary<string, InventoryStockLevel> Load(
        string path,
        DateOnly businessDate,
        IEnumerable<MenuItem> menuItems)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(menuItems);

        InventoryDocument? document = null;
        if (File.Exists(path))
        {
            try
            {
                document = JsonSerializer.Deserialize<InventoryDocument>(
                    File.ReadAllText(path),
                    Options);
                if (document?.Items is null)
                    throw new InvalidDataException($"'{path}' does not contain inventory items.");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"'{path}' is not a valid inventory file.", exception);
            }
        }

        var saved = document?.Items ?? new Dictionary<string, InventoryStockLevel>(StringComparer.Ordinal);
        var isNewBusinessDay = document is null || document.BusinessDate != businessDate;
        var levels = new Dictionary<string, InventoryStockLevel>(StringComparer.Ordinal);
        foreach (var item in menuItems)
        {
            if (!saved.TryGetValue(item.Id, out var level))
            {
                var starting = item.IsAvailable ? DefaultStartingQuantity : 0;
                level = new InventoryStockLevel(starting, starting);
            }
            else if (level.StartingQuantity < 0 || level.RemainingQuantity < 0
                     || level.RemainingQuantity > level.StartingQuantity)
            {
                throw new InvalidDataException($"Inventory quantity for '{item.DisplayName}' is invalid.");
            }
            else if (isNewBusinessDay)
            {
                level = level with { RemainingQuantity = level.StartingQuantity };
            }

            if (!item.IsAvailable && level.RemainingQuantity > 0)
            {
                level = new InventoryStockLevel(0, 0);
            }

            levels.Add(item.Id, level);
        }

        return levels;
    }

    public static Task SaveAsync(
        string path,
        DateOnly businessDate,
        IReadOnlyDictionary<string, InventoryStockLevel> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(items);
        var document = new InventoryDocument
        {
            BusinessDate = businessDate,
            Items = new Dictionary<string, InventoryStockLevel>(items, StringComparer.Ordinal)
        };

        return AtomicFile.WriteAsync(
            path,
            async (stream, token) =>
                await JsonSerializer.SerializeAsync(stream, document, Options, token),
            cancellationToken);
    }
}
