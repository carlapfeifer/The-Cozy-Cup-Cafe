using System.Text.Json;
using SharpQuest.Contracts;

namespace Capstone.Core.Models;


/// <summary>
/// Maps MenuItem objects to and from JSON format using intermediate DTOs.
/// </summary>
public class MenuItemMapper : IItemMapper
{
    // Configure standard JSON serialization rules (camelCase, indented).
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    
    // Data Transfer Objects for safely structuring JSON data.
    private sealed class MenuItemDto
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public CategoryDto? Category { get; set; }
        public PriceDto? Price { get; set; }
        public string? Size { get; set; }
        public List<string>? Allergens { get; set; }
        public bool? IsAvailable { get; set; }
    }

    private sealed class CategoryDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class PriceDto
    {
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
    }

    // Converts a MenuItem object into a formatted JSON string.
    public string ToJson(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is not MenuItem menuItem)
            throw new ArgumentException(
                $"Only {nameof(MenuItem)} is supported, got {item.GetType().Name}.", nameof(item));

        var dto = new MenuItemDto
        {
            Id = menuItem.Id,
            DisplayName = menuItem.DisplayName,
            Category = new CategoryDto
            {
                Id = menuItem.Category.Id,
                Name = menuItem.Category.Name
            },
            Price = new PriceDto
            {
                Amount = menuItem.Price.Amount,
                Currency = menuItem.Price.Currency
            },
            Size = menuItem.Size?.ToString(),
            // Map active enum flags to a list of string names.
            Allergens = Enum.GetValues<Allergen>()
                .Where(a => a != Allergen.None && menuItem.Allergens.HasFlag(a))
                .Select(a => a.ToString())
                .ToList(),
            IsAvailable = menuItem.IsAvailable
        };

        return JsonSerializer.Serialize(dto, Options);
    }

    // Parses a JSON string back into a validated MenuItem object.
    public IItem? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var dto = JsonSerializer.Deserialize<MenuItemDto>(json, Options);

            if (dto?.Category is null || dto.Price is null
                || dto.Price.Amount is null || dto.Price.Currency is null)
                return null;

            // Validate and parse the item size enum.
            Size? size = null;
            if (dto.Size is not null)
            {
                if (!Enum.TryParse<Size>(dto.Size, ignoreCase: true, out var parsedSize)
                    || !Enum.IsDefined(parsedSize))
                    return null;
                size = parsedSize;
            }

            // Reconstruct the bitwise flag combination from the string list.
            var allergens = Allergen.None;
            if (dto.Allergens is not null)
            {
                foreach (var name in dto.Allergens)
                {
                    if (!Enum.TryParse<Allergen>(name, ignoreCase: true, out var parsed)
                        || !Enum.IsDefined(parsed))
                        return null;
                    allergens |= parsed;
                }
            }

            return new MenuItem(
                dto.Id!,
                dto.DisplayName!,
                new Category(dto.Category.Id!, dto.Category.Name!),
                new Money(dto.Price.Amount.Value, dto.Price.Currency),
                size,
                allergens,
                dto.IsAvailable ?? true);
        }
        catch (JsonException)
        {
            return null;   // Invalid JSON or mismatched types.
        }
        catch (ArgumentException)
        {
            return null;   // Triggered by domain constructor guard clauses.
        }
    }
}