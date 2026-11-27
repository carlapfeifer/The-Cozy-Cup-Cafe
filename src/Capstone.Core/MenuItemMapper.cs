using System.Text.Json;
using SharpQuest.Contracts;

namespace Capstone.Core.Models;

public class MenuItemMapper : IItemMapper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private sealed class MenuItemDto
    {
        public string? Kind { get; set; }
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public CategoryDto? Category { get; set; }
        public PriceDto? Price { get; set; }
        public string? Size { get; set; }
        public List<string>? Allergens { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsHot { get; set; }
        public bool? IsDecaf { get; set; }
        public int? PreparationTimeMinutes { get; set; }
        public string? Sku { get; set; }
        public VatDto? Vat { get; set; }
        public List<MenuItemDto>? Parts { get; set; }
        public string? AvailableFrom { get; set; }
        public string? AvailableUntil { get; set; }
        public List<string>? Ingredients { get; set; }
        public string? Origin { get; set; }
    }

    private sealed class VatDto
    {
        public decimal? DineInPercent { get; set; }
        public decimal? TakeawayPercent { get; set; }
    }

    private sealed class CategoryDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public decimal? VatPercent { get; set; }
        public string? Icon { get; set; }
        public string? DisplayColor { get; set; }
    }

    private sealed class PriceDto
    {
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
    }

    public string ToJson(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is not MenuItem menuItem)
        {
            throw new ArgumentException(
                $"Only {nameof(MenuItem)} is supported, got {item.GetType().Name}.",
                nameof(item));
        }

        return JsonSerializer.Serialize(CreateDto(menuItem), Options);
    }

    public IItem? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var dto = JsonSerializer.Deserialize<MenuItemDto>(json, Options);
            return FromDto(dto);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static IItem? FromDto(MenuItemDto? dto)
    {
        if (dto is null
            || dto.Id is null
            || dto.DisplayName is null
            || dto.Category is null
            || dto.Category.Id is null
            || dto.Category.Name is null
            || dto.Price is null
            || dto.Price.Amount is null
            || dto.Price.Currency is null)
        {
            return null;
        }

        if (dto.Sku is not null &&
            string.IsNullOrWhiteSpace(dto.Sku))
        {
            return null;
        }
        if (!TryParseAvailabilityWindow(dto.AvailableFrom, dto.AvailableUntil, out var availableFrom, out var availableUntil))
            return null;

        var sku = dto.Sku;
        var category = new Category(
            dto.Category.Id,
            dto.Category.Name,
            dto.Category.VatPercent ?? 11m,
            dto.Category.Icon ?? "category",
            dto.Category.DisplayColor ?? "color");
        var price = new Money(
            dto.Price.Amount.Value,
            dto.Price.Currency);
        var vat = dto.Vat is null
            ? new VatInfo(11m, 11m)
            : new VatInfo(
                dto.Vat.DineInPercent ?? 11m,
                dto.Vat.TakeawayPercent ?? 11m);
        var allergens = ParseAllergens(dto.Allergens);
        var isAvailable = dto.IsAvailable ?? true;
        var isFeatured = dto.IsFeatured ?? false;
        var isHot = dto.IsHot ?? true;
        var isDecaf = dto.IsDecaf ?? false;
        var kind = dto.Kind?.Trim().ToLowerInvariant();
        var preparationTimeMinutes = dto.PreparationTimeMinutes ?? 5;
        var origin = Enum.TryParse<ItemOrigin>(dto.Origin, ignoreCase: true, out var parsedOrigin)
            ? parsedOrigin
            : ItemOrigin.MadeInHouse;

        if (kind is not null and not "drink" and not "food" and not "combo")
            return null;

        if (kind == "combo")
        {
            if (dto.Parts is null || dto.Parts.Count == 0 || dto.Size is not null)
                return null;

            var parts = new List<MenuItem>();

            foreach (var partDto in dto.Parts)
            {
                var part = FromDto(partDto);

                if (part is not MenuItem menuPart)
                    return null;

                parts.Add(menuPart);
            }

            return new Combo(
                dto.Id,
                dto.DisplayName,
                category,
                price,
                parts,
                isAvailable,
                isFeatured,
                preparationTimeMinutes,
                sku,
                vat,
                availableFrom,
                availableUntil,
                dto.Ingredients,
                origin);
        }

        if (kind == "drink" || (kind is null && dto.Size is not null))
        {
            if (dto.Size is null
                || !Enum.TryParse<Size>(dto.Size, ignoreCase: true, out var size)
                || !Enum.IsDefined(size))
            {
                return null;
            }

            return new Drink(
                dto.Id,
                dto.DisplayName,
                category,
                price,
                size,
                allergens,
                isAvailable,
                isFeatured,
                isHot,
                preparationTimeMinutes,
                sku,
                vat,
                isDecaf,
                availableFrom,
                availableUntil,
                dto.Ingredients,
                origin);
        }

        if (dto.Size is not null)
            return null;

        return new Food(
            dto.Id,
            dto.DisplayName,
            category,
            price,
            allergens,
            isAvailable,
            isFeatured,
            preparationTimeMinutes,
            sku,
            vat,
            availableFrom,
            availableUntil,
            dto.Ingredients,
            origin);
    }

    private static MenuItemDto CreateDto(MenuItem item)
    {
        var dto = new MenuItemDto
        {
            Sku = item.Sku,
            Id = item.Id,
            DisplayName = item.DisplayName,
            Category = new CategoryDto
            {
                Id = item.Category.Id,
                Name = item.Category.Name,
                VatPercent = item.Category.VatPercent,
                Icon = item.Category.Icon,
                DisplayColor = item.Category.DisplayColor,
            },
            Vat = new VatDto
            {
                DineInPercent = item.Vat.DineInPercent,
                TakeawayPercent = item.Vat.TakeawayPercent
            },
            Price = new PriceDto
            {
                Amount = item.Price.Amount,
                Currency = item.Price.Currency
            },
            Allergens = Enum.GetValues<Allergen>()
                .Where(value => value != Allergen.None)
                .Where(value => item.Allergens.HasFlag(value))
                .Select(value => value.ToString())
                .ToList(),
            IsAvailable = item.IsAvailable,
            IsFeatured = item.IsFeatured,
            PreparationTimeMinutes = item.PreparationTimeMinutes,
            AvailableFrom = item.AvailableFrom?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            AvailableUntil = item.AvailableUntil?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            Ingredients = item.Ingredients.Count > 0 ? item.Ingredients.ToList() : null,
            Origin = item.Origin.ToString()
        };

        switch (item)
        {
            case Drink drink:
                dto.Kind = "drink";
                dto.Size = drink.Size.ToString();
                dto.IsHot = drink.IsHot;
                dto.IsDecaf = drink.IsDecaf;
                break;

            case Food:
                dto.Kind = "food";
                break;

            case Combo combo:
                dto.Kind = "combo";
                dto.Parts = combo.Parts
                    .Select(CreateDto)
                    .ToList();
                break;
        }

        return dto;
    }

    private static Allergen ParseAllergens(List<string>? names)
    {
        var allergens = Allergen.None;

        foreach (var name in names ?? new List<string>())
        {
            if (!Enum.TryParse<Allergen>(
                    name,
                    ignoreCase: true,
                    out var allergen)
                || !Enum.IsDefined(allergen))
            {
                throw new InvalidDataException(
                    $"'{name}' is not a valid allergen.");
            }

            allergens |= allergen;
        }

        return allergens;
    }

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
}