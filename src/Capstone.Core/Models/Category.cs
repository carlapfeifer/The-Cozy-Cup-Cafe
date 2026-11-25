public record Category(
    string Id,
    string Name,
    decimal VatPercent = 11m,
    string Icon = "category",
    string DisplayColor = "#D8B59D")
{
    public string Id { get; init; } = ValidateId(Id);
    public string Name { get; init; } = ValidateName(Name);

    public decimal VatPercent { get; init; } =
        VatPercent is >= 0m and <= 100m
            ? VatPercent
            : throw new ArgumentOutOfRangeException(
                nameof(VatPercent),
                "VAT must be between 0 and 100.");

    public string Icon { get; init; } =
        !string.IsNullOrWhiteSpace(Icon)
            ? Icon
            : throw new ArgumentException("Icon is required.", nameof(Icon));

    public string DisplayColor { get; init; } =
        IsValidColor(DisplayColor)
            ? DisplayColor
            : throw new ArgumentException(
                "DisplayColor must be a hexadecimal color.",
                nameof(DisplayColor));

    private static bool IsValidColor(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith('#')
        && value.Length is 7
        && value[1..].All(Uri.IsHexDigit);

    private static string ValidateId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return id;
    }

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name;
    }
}