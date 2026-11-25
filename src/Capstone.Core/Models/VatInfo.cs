namespace Capstone.Core.Models;

public sealed record VatInfo
{
    public decimal DineInPercent { get; init; }
    public decimal TakeawayPercent { get; init; }

    public VatInfo(decimal dineInPercent, decimal takeawayPercent)
    {
        ValidatePercent(dineInPercent);
        ValidatePercent(takeawayPercent);

        DineInPercent = dineInPercent;
        TakeawayPercent = takeawayPercent;
    }

    public decimal PercentFor(ServiceMode mode) => mode switch
    {
        ServiceMode.DineIn => DineInPercent,
        ServiceMode.Takeaway => TakeawayPercent,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static void ValidatePercent(decimal percent)
    {
        if (percent is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percent),
                "VAT must be between 0 and 100.");
        }
    }
}