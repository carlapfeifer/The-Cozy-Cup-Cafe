using System.ComponentModel;
using System.Runtime.CompilerServices;
using Capstone.Core.Models;

namespace Capstone.Core.ViewModels;

public sealed class InventoryItemRow(MenuItem item, int startingQuantity) : INotifyPropertyChanged
{
    private string _startingQuantityInput = startingQuantity.ToString();
    private int _remainingQuantity = startingQuantity;
    private string _stockStatus = "20 in stock";

    public event PropertyChangedEventHandler? PropertyChanged;

    public MenuItem Item { get; } = item;
    public string DisplayName => Item.DisplayName;
    public string CategoryName => Item.Category.Name;
    public string Sku => Item.Sku;
    public ItemOrigin Origin => Item.Origin;
    public string OriginDisplay => Origin == ItemOrigin.SupplierBought ? "Supplier Bought" : "Made in House";

    public string StartingQuantityInput
    {
        get => _startingQuantityInput;
        set
        {
            if (_startingQuantityInput == value)
                return;
            _startingQuantityInput = value;
            OnPropertyChanged();
        }
    }

    public int RemainingQuantity
    {
        get => _remainingQuantity;
        private set
        {
            if (_remainingQuantity == value)
                return;
            _remainingQuantity = value;
            OnPropertyChanged();
        }
    }

    public string StockStatus
    {
        get => _stockStatus;
        private set
        {
            if (_stockStatus == value)
                return;
            _stockStatus = value;
            OnPropertyChanged();
        }
    }

    public bool HasValidStartingQuantity =>
        int.TryParse(StartingQuantityInput, out var quantity) && quantity >= 0;

    public int ParsedStartingQuantity =>
        int.TryParse(StartingQuantityInput, out var quantity) && quantity >= 0
            ? quantity
            : throw new InvalidOperationException("Enter a non-negative whole number for starting stock.");

    public void UpdateStock(int remainingQuantity, int startingQuantity)
    {
        RemainingQuantity = remainingQuantity;
        StartingQuantityInput = startingQuantity.ToString();
        StockStatus = remainingQuantity switch
        {
            0 => "SOLD OUT",
            <= 2 => $"Almost sold out — only {remainingQuantity} left",
            <= 5 => $"Low stock — only {remainingQuantity} left",
            _ => $"{remainingQuantity} in stock"
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
