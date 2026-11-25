using System.Text.Json;
using Capstone.Core.Models;

namespace Capstone.Core.Storage;

public static class CompletedOrderHistoryFile
{
    private sealed class HistoryDocument
    {
        public int FormatVersion { get; set; } = 1;
        public int Capacity { get; set; }
        public List<OrderRow> Orders { get; set; } = new();
    }

    private sealed class OrderRow
    {
        public string? Currency { get; set; }
        public DateTime? PlacedAt { get; set; }
        public decimal? DiscountPercent { get; set; }
        public string? ServiceMode { get; set; }
        public string? PaymentMethod { get; set; }
        public List<LineRow>? Lines { get; set; }
    }

    private sealed class LineRow
    {
        public MenuFile.ItemRow? Item { get; set; }
        public int? Quantity { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static Task SaveAsync(
        CompletedOrderHistory history,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var document = new HistoryDocument
        {
            Capacity = history.Capacity,
            // Record() pushes newest first, so load oldest first to preserve that order.
            Orders = history.Orders
                .Reverse()
                .Select(order => new OrderRow
                {
                    Currency = order.Currency,
                    PlacedAt = order.PlacedAt,
                    DiscountPercent = order.DiscountPercent,
                    ServiceMode = order.ServiceMode.ToString(),
                    PaymentMethod = order.PaymentMethod.ToString(),
                    Lines = order.Lines.Select(line => new LineRow
                    {
                        Item = MenuFile.ToRow(line.Item),
                        Quantity = line.Quantity
                    }).ToList()
                })
                .ToList()
        };

        return AtomicFile.WriteAsync(
            path,
            async (stream, token) =>
                await JsonSerializer.SerializeAsync(stream, document, Options, token),
            cancellationToken);
    }

    public static async Task<CompletedOrderHistory> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

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
            return new CompletedOrderHistory();
        }
        catch (DirectoryNotFoundException)
        {
            return new CompletedOrderHistory();
        }

        try
        {
            await using (stream)
            {
                var document = await JsonSerializer.DeserializeAsync<HistoryDocument>(
                    stream, Options, cancellationToken)
                    ?? throw new InvalidDataException("The order-history file is empty.");

                if (document.FormatVersion != 1 || document.Capacity < 1)
                    throw new InvalidDataException("The order-history file has invalid metadata.");

                var history = new CompletedOrderHistory(document.Capacity);

                foreach (var savedOrder in document.Orders
                             ?? throw new InvalidDataException("The order-history file has no orders."))
                {
                    if (savedOrder.Currency is null
                        || savedOrder.PlacedAt is null
                        || savedOrder.ServiceMode is null
                        || savedOrder.PaymentMethod is null
                        || !Enum.TryParse<ServiceMode>(
                            savedOrder.ServiceMode, true, out var serviceMode)
                        || !Enum.IsDefined(serviceMode)
                        || !Enum.TryParse<PaymentMethod>(
                            savedOrder.PaymentMethod, true, out var paymentMethod)
                        || !Enum.IsDefined(paymentMethod))
                    {
                        throw new InvalidDataException("An order in the history file is invalid.");
                    }

                    var order = new Order(
                        savedOrder.Currency,
                        savedOrder.PlacedAt,
                        serviceMode,
                        paymentMethod);

                    foreach (var savedLine in savedOrder.Lines
                                 ?? throw new InvalidDataException("An order has no lines."))
                    {
                        if (savedLine.Item is null || savedLine.Quantity is null)
                            throw new InvalidDataException("An order line is missing required data.");

                        order.Add(
                            MenuFile.FromRow(savedLine.Item),
                            savedLine.Quantity.Value);
                    }

                    if (savedOrder.DiscountPercent is not null)
                        order.ApplyDiscount(savedOrder.DiscountPercent.Value);

                    history.Record(order);
                }

                return history;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The order-history file is invalid JSON.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"The order-history file contains invalid data: {ex.Message}", ex);
        }
    }
}