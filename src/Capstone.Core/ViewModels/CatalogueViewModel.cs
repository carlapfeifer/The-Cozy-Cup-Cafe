using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using SharpQuest.Contracts;
using Capstone.Core.Currency;
using Capstone.Core.Models;
using Capstone.Core.Payments;
using Capstone.Core.Storage;

namespace Capstone.Core.ViewModels;

public sealed class CatalogueViewModel : ICatalogueViewModel
{
    private static readonly HttpClient SharedExchangeHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly CafeStore _store;
    private readonly ObservableCollection<IItem> _items = [];
    private readonly ObservableCollection<MenuCategorySection> _menuSections = [];
    private readonly RelayCommand _removeCommand;
    private readonly RelayCommand _selectMenuItemCommand;
    private readonly RelayCommand _applyDiscountCommand;
    private readonly AsyncRelayCommand _convertOrderCommand;
    private readonly AsyncRelayCommand _checkoutCommand;
    private readonly AsyncRelayCommand _completeOrderCommand;
    private readonly AsyncRelayCommand _saveStartingStockCommand;
    private readonly CurrencyConverter _currencyConverter;
    private readonly string _completedOrdersPath;
    private readonly string _reportsDirectory;
    private readonly string _inventoryPath;
    private DateOnly _businessDate;
    private readonly Dictionary<string, InventoryStockLevel> _inventoryLevels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _reservedQuantities = new(StringComparer.Ordinal);
    private readonly ObservableCollection<InventoryItemRow> _inventoryItems = [];
    private readonly RelayCommand _showHomeCommand;
    private readonly RelayCommand _showOrderCommand;
    private readonly RelayCommand _showInventoryCommand;
    private readonly RelayCommand _selectCategoryCommand;
    private string _filter = "";
    private string _categoryFilter = "All categories";
    private string _originFilter = "All";
    private IItem? _selected;
    private Order _order = new("RON");
    private readonly ObservableCollection<OrderLine> _orderLines = [];
    private readonly RelayCommand _addToOrderCommand;
    private readonly RelayCommand _removeOrderLineCommand;
    private OrderLine? _selectedOrderLine;
    private readonly RelayCommand _removeOneOrderItemCommand;
    private readonly RecentlyUsed<MenuItem> _recentlyUsed = new(5);
    private readonly ObservableCollection<MenuItem> _recentItems = [];
    private readonly ObservableCollection<MenuItem> _mostOrderedItems = [];
    private readonly Dictionary<string, int> _orderCounts = new(StringComparer.Ordinal);
    private string _discountInput = "0";
    private string _targetCurrency = "EUR";
    private string _paymentCurrency = "RON";
    private Money? _convertedOrderTotal;
    private string _operationMessage = "";
    private string _selectedServiceMode = "Dine-in";
    private string _selectedPaymentMethod = "Cash";
    private string? _lastCompletedReceipt;
    private DateTime _currentTime = DateTime.Now;
    private string _currentPage = "Home";
    private bool _currentOrderStockCommitted;

    private MilkOption _selectedMilkOption = MilkOption.Regular;
    private SyrupOption _selectedSyrupOption = SyrupOption.None;
    private SugarOption _selectedSugarOption = SugarOption.None;
    private BreadOption _selectedBreadOption = BreadOption.Standard;

    public IReadOnlyList<MilkOption> AvailableMilkOptions { get; } = Enum.GetValues<MilkOption>();
    public IReadOnlyList<SyrupOption> AvailableSyrupOptions { get; } = Enum.GetValues<SyrupOption>();
    public IReadOnlyList<SugarOption> AvailableSugarOptions { get; } = Enum.GetValues<SugarOption>();
    public IReadOnlyList<BreadOption> AvailableBreadOptions { get; } = Enum.GetValues<BreadOption>();

    public MilkOption SelectedMilkOption
    {
        get => _selectedMilkOption;
        set
        {
            if (_selectedMilkOption == value)
                return;
            _selectedMilkOption = value;
            OnPropertyChanged();
        }
    }

    public SyrupOption SelectedSyrupOption
    {
        get => _selectedSyrupOption;
        set
        {
            if (_selectedSyrupOption == value)
                return;
            _selectedSyrupOption = value;
            OnPropertyChanged();
        }
    }

    public SugarOption SelectedSugarOption
    {
        get => _selectedSugarOption;
        set
        {
            if (_selectedSugarOption == value)
                return;
            _selectedSugarOption = value;
            OnPropertyChanged();
        }
    }

    public BreadOption SelectedBreadOption
    {
        get => _selectedBreadOption;
        set
        {
            if (_selectedBreadOption == value)
                return;
            _selectedBreadOption = value;
            OnPropertyChanged();
        }
    }

    public bool CanCustomizeSelected => CanCustomizeMilk || CanCustomizeSyrup || CanCustomizeSugar || CanCustomizeBread;

    public bool CanCustomizeMilk => Selected is MenuItem item
        && item.Origin != ItemOrigin.SupplierBought
        && ((item.Ingredients.Any(i => i.Contains("milk", StringComparison.OrdinalIgnoreCase)
                                   || i.Contains("dairy", StringComparison.OrdinalIgnoreCase)
                                   || i.Contains("cream", StringComparison.OrdinalIgnoreCase)))
            || (item.Ingredients.Count == 0 && item.Allergens.HasFlag(Allergen.Milk)));

    public bool CanCustomizeSyrup => Selected is Drink;

    public bool CanCustomizeSugar => Selected is Drink;

    public bool CanCustomizeBread => Selected is MenuItem item
        && item.Origin != ItemOrigin.SupplierBought
        && IsBreadItem(item);

    private static bool IsBreadItem(MenuItem item)
    {
        if (item is Combo combo)
            return combo.Parts.Any(IsBreadItem);

        var name = item.DisplayName;
        var category = item.Category.Name;

        return name.Contains("sandwich", StringComparison.OrdinalIgnoreCase)
            || name.Contains("toast", StringComparison.OrdinalIgnoreCase)
            || name.Contains("bagel", StringComparison.OrdinalIgnoreCase)
            || name.Contains("panini", StringComparison.OrdinalIgnoreCase)
            || name.Contains("wrap", StringComparison.OrdinalIgnoreCase)
            || name.Contains("bread", StringComparison.OrdinalIgnoreCase)
            || name.Contains("croissant", StringComparison.OrdinalIgnoreCase)
            || name.Contains("roll", StringComparison.OrdinalIgnoreCase)
            || name.Contains("scone", StringComparison.OrdinalIgnoreCase)
            || name.Contains("bun", StringComparison.OrdinalIgnoreCase)
            || category.Contains("sandwich", StringComparison.OrdinalIgnoreCase);
    }

    public CatalogueViewModel(
        IItemStore store,
        IExchangeRateProvider? exchangeRateProvider = null,
        string? completedOrdersPath = null,
        string? reportsDirectory = null,
        string? inventoryPath = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store as CafeStore
            ?? throw new ArgumentException("The catalogue requires a CafeStore.", nameof(store));
        _currencyConverter = new CurrencyConverter(
            exchangeRateProvider
            ?? new FrankfurterRateProvider(SharedExchangeHttpClient));
        _completedOrdersPath = completedOrdersPath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheCozyCupCafe",
                "completed-orders.json");
        _reportsDirectory = reportsDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TheCozyCupCafe",
                "Reports");
        _inventoryPath = inventoryPath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheCozyCupCafe",
                "inventory.json");
        _businessDate = DateOnly.FromDateTime(DateTime.Now);
        _inventoryLevels = InventoryFile.Load(
            _inventoryPath,
            _businessDate,
            _store.All().OfType<MenuItem>());

        _showHomeCommand = new RelayCommand(_ => CurrentPage = "Home");
        _showOrderCommand = new RelayCommand(_ => CurrentPage = "Order");
        _showInventoryCommand = new RelayCommand(_ => CurrentPage = "Inventory");
        _removeCommand = new RelayCommand(
            _ => RemoveSelected(),
            _ => Selected is not null);
        _selectMenuItemCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is MenuItem item)
                    Selected = item;
            });
        _selectCategoryCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is string category && Categories.Contains(category))
                    CategoryFilter = category;
            });
        _applyDiscountCommand = new RelayCommand(
            _ => ApplyDiscount(),
            parameter => _orderLines.Count > 0 && TryGetDiscountInput(out _));
        _convertOrderCommand = new AsyncRelayCommand(
            _ => ConvertOrderAsync(),
            _ => _orderLines.Count > 0);
        _checkoutCommand = new AsyncRelayCommand(
            _ => CheckoutAsync(),
            _ => _orderLines.Count > 0);
        _completeOrderCommand = new AsyncRelayCommand(
            _ => CompleteOrderAsync());
        _saveStartingStockCommand = new AsyncRelayCommand(
            SetStartingStockAsync,
            parameter => parameter is InventoryItemRow row
                         && !_orderLines.Any(line => line.Item.Id == row.Item.Id));
        _convertOrderCommand.ExecutionFailed += exception =>
            OperationMessage = $"Currency conversion failed: {exception.Message}";
        _completeOrderCommand.ExecutionFailed += exception =>
            OperationMessage = $"Could not save or export the end-of-day report: {exception.Message}";
        _saveStartingStockCommand.ExecutionFailed += exception =>
            OperationMessage = $"Could not save inventory: {exception.Message}";
        _addToOrderCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is MenuItem item)
                {
                    AddItemToOrder(item, SelectedMilkOption, SelectedSyrupOption, SelectedSugarOption, SelectedBreadOption);
                }
            },
            parameter => parameter is MenuItem);
        _removeOrderLineCommand = new RelayCommand(
            _ => RemoveSelectedOrderLine(),
            _ => SelectedOrderLine is not null);
        _removeOneOrderItemCommand = new RelayCommand(
            _ => RemoveOneFromSelectedOrderLine(),
            _ => SelectedOrderLine is not null);

        AddCommand = new RelayCommand(
            parameter =>
            {
                if (parameter is IItem item)
                    _store.Add(item);
            });

        _store.ItemAdded += OnItemAdded;
        _store.ItemRemoved += OnItemRemoved;
        _store.ItemUpdated += OnItemUpdated;
        _store.AvailabilityChanged += OnItemAvailabilityChanged;

        foreach (var item in _store.All())
        {
            if (Matches(item))
                _items.Add(item);

            if (item is MenuItem menuItem
                && _inventoryLevels.TryGetValue(menuItem.Id, out var level))
            {
                var row = new InventoryItemRow(menuItem, level.StartingQuantity);
                row.UpdateStock(level.RemainingQuantity, level.StartingQuantity);
                _inventoryItems.Add(row);
            }
        }

        RefreshMenuSections();
        RefreshMostOrderedItems();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IEnumerable<IItem> Items => _items;
    public ObservableCollection<InventoryItemRow> InventoryItems => _inventoryItems;

    public string CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (_currentPage == value)
                return;
            _currentPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsHomePage));
            OnPropertyChanged(nameof(IsOrderPage));
            OnPropertyChanged(nameof(IsInventoryPage));
        }
    }

    public bool IsHomePage => CurrentPage == "Home";
    public bool IsOrderPage => CurrentPage == "Order";
    public bool IsInventoryPage => CurrentPage == "Inventory";

    public string Filter
    {
        get => _filter;
        set
        {
            var next = value ?? "";
            if (_filter == next)
                return;

            _filter = next;
            OnPropertyChanged();
            Refill();
        }
    }
    public IReadOnlyList<string> Categories =>
        new[] { "All categories" }
            .Concat(_store.All()
                .OfType<MenuItem>()
                .Select(item => item.Category.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            .ToArray();

    public ObservableCollection<MenuItem> MostOrderedItems => _mostOrderedItems;

    public ObservableCollection<MenuCategorySection> MenuSections => _menuSections;

    public System.Windows.Input.ICommand SelectMenuItemCommand =>
        _selectMenuItemCommand;

    public System.Windows.Input.ICommand SelectCategoryCommand =>
        _selectCategoryCommand;

    public IReadOnlyList<string> OriginFilters { get; } = ["All", "Made in House", "Supplier Bought"];

    public string OriginFilter
    {
        get => _originFilter;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (_originFilter == next)
                return;

            _originFilter = next;
            OnPropertyChanged();
            Refill();
        }
    }

    public string CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? "All categories" : value;
            if (_categoryFilter == next)
                return;

            _categoryFilter = next;
            OnPropertyChanged();
            Refill();
        }
    }

    public IItem? Selected
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value))
                return;

            _selected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCustomizeSelected));
            OnPropertyChanged(nameof(CanCustomizeMilk));
            OnPropertyChanged(nameof(CanCustomizeSyrup));
            OnPropertyChanged(nameof(CanCustomizeSugar));
            OnPropertyChanged(nameof(CanCustomizeBread));
            OnPropertyChanged(nameof(Details));
            OnPropertyChanged(nameof(DetailsCategory));
            OnPropertyChanged(nameof(DetailsOrigin));
            OnPropertyChanged(nameof(DetailsPrice));
            OnPropertyChanged(nameof(DetailsAvailability));
            OnPropertyChanged(nameof(DetailsStockStatus));
            OnPropertyChanged(nameof(DetailsAllergens));
            OnPropertyChanged(nameof(DetailsIngredients));
            OnPropertyChanged(nameof(DetailsMore));
            _removeCommand.RaiseCanExecuteChanged();
        }
    }

    public string Details => Selected is null
        ? "No item selected"
        : Selected is MenuItem item
            ? DescribeMenuItem(item)
            : Selected.DisplayName;

    public string DetailsCategory => Selected is MenuItem item
        ? item.Category.Name
        : "Choose a menu item";

    public string DetailsOrigin => Selected is MenuItem item
        ? item.Origin == ItemOrigin.SupplierBought ? "Supplier Bought" : "Made in House"
        : "—";

    public string DetailsPrice => Selected is MenuItem item
        ? item.FinalPrice(TimeOnly.FromDateTime(_currentTime)) == item.Price.Amount
            ? $"{item.Price.Amount:0.00} {item.Price.Currency}"
            : $"{item.FinalPrice(TimeOnly.FromDateTime(_currentTime)):0.00} {item.Price.Currency} (time-based price)"
        : "—";

    public string DetailsAvailability => Selected is MenuItem item
        ? !item.IsAvailable
            ? "Sold out"
            : item.IsAvailableAt(TimeOnly.FromDateTime(_currentTime))
                ? item.AvailabilityWindow.Length == 0
                    ? "Available"
                    : $"Available now · {item.AvailabilityWindow}"
                : item.AvailabilityWindow.Length == 0
                    ? "Unavailable"
                    : $"Not available now · available {item.AvailabilityWindow}"
        : "—";

    public string DetailsStockStatus => Selected is MenuItem item
        ? GetStockStatus(AvailableStock(item))
        : "—";

    public string DetailsAllergens => Selected is MenuItem item
        ? item.Allergens == Allergen.None ? "None" : item.Allergens.ToString()
        : "—";

    public string DetailsIngredients => Selected is MenuItem item && item.Ingredients.Count > 0
        ? string.Join(", ", item.Ingredients)
        : "None specified";

    public string DetailsMore => Selected is MenuItem item
        ? string.Join(Environment.NewLine, new[]
        {
            $"SKU: {item.Sku}",
            $"Origin: {(item.Origin == ItemOrigin.SupplierBought ? "Supplier Bought" : "Made in House")}",
            $"Preparation: {item.PreparationTimeMinutes} min",
            item.Ingredients.Count > 0
                ? $"Ingredients: {string.Join(", ", item.Ingredients)}"
                : null,
            item.AvailabilityWindow.Length > 0
                ? $"Available hours: {item.AvailabilityWindow}"
                : null,
            item.IsFeatured ? "Featured" : null,
            item is Drink drink
                ? $"Size: {drink.Size} · {(drink.IsHot ? "Hot" : "Cold")}{(drink.IsDecaf ? " · Decaf" : "")}"
                : null,
            item is Combo combo
                ? $"Includes: {string.Join(", ", combo.Parts.Select(part => part.DisplayName))}"
                : null
        }.Where(line => line is not null))
        : "";

    private static string DescribeMenuItem(MenuItem item)
    {
        var lines = new List<string>
        {
            item.DisplayName,
            $"Category: {item.Category.Name}",
            $"Origin: {(item.Origin == ItemOrigin.SupplierBought ? "Supplier Bought" : "Made in House")}",
            $"Price: {item.Price.Amount:0.00} {item.Price.Currency}",
            $"Availability: {(item.IsAvailable ? "Available" : "Sold out")}",
            $"Allergens: {(item.Allergens == Allergen.None ? "None" : item.Allergens.ToString())}",
            $"Ingredients: {(item.Ingredients.Count > 0 ? string.Join(", ", item.Ingredients) : "None specified")}",
            $"SKU: {item.Sku}",
            $"Preparation: {item.PreparationTimeMinutes} min"
        };

        if (item.IsFeatured)
            lines.Add("Featured");

        if (item is Drink drink)
        {
            lines.Add($"Size: {drink.Size}");
            lines.Add($"Temperature: {(drink.IsHot ? "Hot" : "Cold")}");
            if (drink.IsDecaf)
                lines.Add("Decaf");
        }

        if (item is Combo combo)
            lines.Add($"Includes: {string.Join(", ", combo.Parts.Select(part => part.DisplayName))}");

        return string.Join(Environment.NewLine, lines);
    }

    public int Count => _items.Count;
    public ObservableCollection<OrderLine> OrderLines => _orderLines;
    public ObservableCollection<MenuItem> RecentlyUsedItems => _recentItems;

    public IReadOnlyList<string> ServiceModes { get; } = ["Dine-in", "Takeaway"];
    public IReadOnlyList<string> PaymentMethods { get; } = ["Cash", "Card", "Mobile wallet"];

    public string SelectedServiceMode
    {
        get => _selectedServiceMode;
        set
        {
            var mode = value switch
            {
                "Dine-in" => ServiceMode.DineIn,
                "Takeaway" => ServiceMode.Takeaway,
                _ => throw new ArgumentException("Choose a supported service mode.", nameof(value))
            };
            if (_selectedServiceMode == value)
                return;

            _selectedServiceMode = value;
            _order.SetServiceDetails(mode, ParsePaymentMethod(_selectedPaymentMethod));
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReceiptPreview));
            OnPropertyChanged(nameof(OrderVatTotal));
        }
    }

    public string SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            var method = ParsePaymentMethod(value);
            if (_selectedPaymentMethod == value)
                return;

            _selectedPaymentMethod = value;
            _order.SetServiceDetails(ParseServiceMode(_selectedServiceMode), method);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReceiptPreview));
        }
    }

    public decimal OrderTotal => _order.Total;

    public string OrderCurrency => _order.Currency;

    public decimal OrderVatTotal => _order.VatTotal;

    public decimal AppliedDiscountPercent => _order.DiscountPercent;

    public string DiscountInput
    {
        get => _discountInput;
        set
        {
            var next = value ?? "";
            if (_discountInput == next)
                return;

            _discountInput = next;
            OnPropertyChanged();
            _applyDiscountCommand.RaiseCanExecuteChanged();
        }
    }

    public IReadOnlyList<string> SupportedCurrencies { get; } = ["RON", "EUR", "USD"];

    public string TargetCurrency
    {
        get => _targetCurrency;
        set
        {
            if (!SupportedCurrencies.Contains(value, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a supported currency.", nameof(value));

            var next = value.ToUpperInvariant();
            if (_targetCurrency == next)
                return;

            _targetCurrency = next;
            OnPropertyChanged();
            ClearConvertedTotal();
        }
    }

    public string PaymentCurrency
    {
        get => _paymentCurrency;
        set
        {
            if (!SupportedCurrencies.Contains(value, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a supported payment currency.", nameof(value));

            var next = value.ToUpperInvariant();
            if (_paymentCurrency == next)
                return;

            _paymentCurrency = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReceiptPreview));
        }
    }

    public string ConvertedOrderDisplay => _convertedOrderTotal is { } amount
        ? $"{amount.Amount:0.00} {amount.Currency}"
        : "Not converted";

    public string ReceiptPreview => _orderLines.Count == 0 && _lastCompletedReceipt is not null
        ? _lastCompletedReceipt
        : $"{CafeReportFormatter.FormatReceipt(_order)}Payment currency: {PaymentCurrency}{Environment.NewLine}";

    public string EndOfDayExportPath => Path.Combine(
        _reportsDirectory,
        $"end-of-day-{DateOnly.FromDateTime(DateTime.Now):yyyy-MM-dd}.txt");

    public string OperationMessage
    {
        get => _operationMessage;
        private set
        {
            if (_operationMessage == value)
                return;

            _operationMessage = value;
            OnPropertyChanged();
        }
    }

    public string CurrentTimeDisplay =>
        $"Café time: {_currentTime.ToString("dddd, HH:mm:ss", CultureInfo.CurrentCulture)}";

    public System.Windows.Input.ICommand ShowHomeCommand => _showHomeCommand;
    public System.Windows.Input.ICommand ShowOrderCommand => _showOrderCommand;
    public System.Windows.Input.ICommand ShowInventoryCommand => _showInventoryCommand;
    public System.Windows.Input.ICommand SaveStartingStockCommand => _saveStartingStockCommand;

    public void UpdateClock(DateTime currentTime)
    {
        _currentTime = currentTime;
        var currentDate = DateOnly.FromDateTime(currentTime);
        if (currentDate != _businessDate && _orderLines.Count == 0)
        {
            _businessDate = currentDate;
            var freshLevels = InventoryFile.Load(
                _inventoryPath,
                _businessDate,
                _store.All().OfType<MenuItem>());
            _inventoryLevels.Clear();
            foreach (var pair in freshLevels)
                _inventoryLevels.Add(pair.Key, pair.Value);
            RefreshInventoryRows();
            OnPropertyChanged(nameof(InventoryBusinessDate));
        }
        OnPropertyChanged(nameof(CurrentTimeDisplay));
        OnPropertyChanged(nameof(DetailsAvailability));
        OnPropertyChanged(nameof(DetailsStockStatus));
        OnPropertyChanged(nameof(DetailsPrice));
        _addToOrderCommand.RaiseCanExecuteChanged();
    }

    public System.Windows.Input.ICommand AddToOrderCommand => _addToOrderCommand;

    public System.Windows.Input.ICommand AddCommand { get; }

    public System.Windows.Input.ICommand RemoveCommand => _removeCommand;
    public System.Windows.Input.ICommand ApplyDiscountCommand => _applyDiscountCommand;
    public System.Windows.Input.ICommand ConvertOrderCommand => _convertOrderCommand;
    public System.Windows.Input.ICommand CheckoutCommand => _checkoutCommand;
    public System.Windows.Input.ICommand CompleteOrderCommand => _completeOrderCommand;

    public string InventoryBusinessDate =>
        $"Starting stock for {_businessDate:dddd, MMMM d}";

    public void AddItemToOrder(MenuItem item) =>
        AddItemToOrder(item, MilkOption.Regular, SyrupOption.None, SugarOption.None, BreadOption.Standard);

    public void AddItemToOrder(MenuItem item, MilkOption milk, SyrupOption syrup) =>
        AddItemToOrder(item, milk, syrup, SugarOption.None, BreadOption.Standard);

    public void AddItemToOrder(
        MenuItem item,
        MilkOption milk,
        SyrupOption syrup,
        SugarOption sugar,
        BreadOption bread)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_currentOrderStockCommitted)
        {
            OperationMessage = "This order has already reduced inventory. Finish it before adding more items.";
            return;
        }

        if (item is Combo combo)
        {
            if (!combo.IsAvailable || !combo.IsAvailableAt(TimeOnly.FromDateTime(_currentTime)))
            {
                Selected = combo;
                var unavailablePart = combo.Parts.FirstOrDefault(p => !p.IsAvailable);
                if (unavailablePart is not null)
                {
                    OperationMessage = $"{combo.DisplayName} is unavailable because ingredient '{unavailablePart.DisplayName}' is sold out.";
                }
                else if (!combo.IsAvailableAt(TimeOnly.FromDateTime(_currentTime)))
                {
                    OperationMessage = combo.AvailabilityWindow.Length == 0
                        ? $"{combo.DisplayName} is unavailable."
                        : $"{combo.DisplayName} is not available now. Available {combo.AvailabilityWindow}.";
                }
                else
                {
                    OperationMessage = $"{combo.DisplayName} is unavailable.";
                }
                return;
            }
        }

        if (!item.IsAvailable)
        {
            Selected = item;
            OperationMessage = $"{item.DisplayName} is sold out.";
            return;
        }

        if (!item.IsAvailableAt(TimeOnly.FromDateTime(_currentTime)))
        {
            Selected = item;
            OperationMessage = item.AvailabilityWindow.Length == 0
                ? $"{item.DisplayName} is unavailable."
                : $"{item.DisplayName} is not available now. Available {item.AvailabilityWindow}.";
            return;
        }

        if (AvailableStock(item) <= 0)
        {
            Selected = item;
            OperationMessage = $"{item.DisplayName} is sold out for today.";
            RefreshInventoryRows();
            return;
        }

        if (!string.Equals(item.Price.Currency, _order.Currency, StringComparison.OrdinalIgnoreCase))
        {
            OperationMessage = $"{item.DisplayName} is priced in {item.Price.Currency}, but this order uses {_order.Currency}.";
            return;
        }

        if (_orderLines.Count == 0)
            _order = CreateNewOrder(_currentTime);

        var actualMilk = CanCustomizeMilk ? milk : MilkOption.Regular;
        var actualSyrup = item is Drink ? syrup : SyrupOption.None;
        var actualSugar = item is Drink ? sugar : SugarOption.None;
        var actualBread = (item.Origin != ItemOrigin.SupplierBought && IsBreadItem(item)) ? bread : BreadOption.Standard;

        _order.Add(item, 1, actualMilk, actualSyrup, actualSugar, actualBread);
        _reservedQuantities[item.Id] = _reservedQuantities.GetValueOrDefault(item.Id) + 1;
        _lastCompletedReceipt = null;
        _orderCounts[item.Id] = _orderCounts.GetValueOrDefault(item.Id) + 1;
        _recentlyUsed.Push(item);
        RefreshRecentlyUsedItems();

        var updatedLine = _order.Lines.First(line =>
            line.Item.Id == item.Id
            && line.Milk == actualMilk
            && line.Syrup == actualSyrup
            && line.Sugar == actualSugar
            && line.Bread == actualBread);

        var displayedIndex = -1;
        for (var index = 0; index < _orderLines.Count; index++)
        {
            if (_orderLines[index].Item.Id == item.Id
                && _orderLines[index].Milk == actualMilk
                && _orderLines[index].Syrup == actualSyrup
                && _orderLines[index].Sugar == actualSugar
                && _orderLines[index].Bread == actualBread)
            {
                displayedIndex = index;
                break;
            }
        }

        if (displayedIndex >= 0)
            _orderLines[displayedIndex] = updatedLine;
        else
            _orderLines.Add(updatedLine);

        RefreshInventoryRows();
        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(OrderVatTotal));
        OnPropertyChanged(nameof(AppliedDiscountPercent));
        OnPropertyChanged(nameof(ReceiptPreview));
        ClearConvertedTotal();
        RaiseOrderCommandsCanExecuteChanged();
        var remaining = AvailableStock(item);
        OperationMessage = remaining is > 0 and <= 5
            ? $"Low stock: only {remaining} {item.DisplayName} left."
            : "";
    }

    private void OnItemAdded(IItem item)
    {
        RefreshMostOrderedItems();
        if (item is MenuItem menuItem && !_inventoryLevels.ContainsKey(menuItem.Id))
        {
            var starting = menuItem.IsAvailable ? 20 : 0;
            var level = new InventoryStockLevel(starting, starting);
            _inventoryLevels.Add(menuItem.Id, level);
            var row = new InventoryItemRow(menuItem, level.StartingQuantity);
            row.UpdateStock(level.RemainingQuantity, level.StartingQuantity);
            _inventoryItems.Add(row);
        }
        if (!Matches(item))
            return;

        var previousCount = Count;
        _items.Add(item);
        NotifyCountIfChanged(previousCount);
        OnPropertyChanged(nameof(Categories));
        RefreshMenuSections();
    }

    private void OnItemRemoved(IItem item)
    {
        var previousCount = Count;
        var shownItem = _items.FirstOrDefault(existing => existing.Id == item.Id);
        if (shownItem is not null)
            _items.Remove(shownItem);
        if (item is MenuItem menuItem && !_orderLines.Any(line => line.Item.Id == menuItem.Id))
        {
            var inventoryRow = _inventoryItems.FirstOrDefault(row => row.Item.Id == menuItem.Id);
            if (inventoryRow is not null)
                _inventoryItems.Remove(inventoryRow);
            _inventoryLevels.Remove(menuItem.Id);
        }

        if (Selected?.Id == item.Id)
            Selected = null;

        NotifyCountIfChanged(previousCount);
        OnPropertyChanged(nameof(Categories));
        RefreshMenuSections();
        RefreshMostOrderedItems();
    }

    private void RemoveSelected()
    {
        if (Selected is not { } selected)
            return;

        _store.Remove(selected.Id);
        Selected = null;
    }
    private void RemoveSelectedOrderLine()
    {
        if (SelectedOrderLine is not { } line)
            return;

        _order.RemoveLine(line);
        ReleaseReservedStock(line.Item.Id, line.Quantity);
        _orderLines.Remove(line);
        SelectedOrderLine = null;
        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(OrderVatTotal));
        OnPropertyChanged(nameof(AppliedDiscountPercent));
        OnPropertyChanged(nameof(ReceiptPreview));
        RefreshInventoryRows();
        ClearConvertedTotal();
        RaiseOrderCommandsCanExecuteChanged();
    }

    private void Refill()
    {
        var previousCount = Count;
        _items.Clear();

        foreach (var item in _store.All())
        {
            if (Matches(item))
                _items.Add(item);
        }

        RefreshMenuSections();
        NotifyCountIfChanged(previousCount);
    }
    public OrderLine? SelectedOrderLine
    {
        get => _selectedOrderLine;
        set
        {
            if (ReferenceEquals(_selectedOrderLine, value))
                return;

            _selectedOrderLine = value;
            OnPropertyChanged();
            _removeOrderLineCommand.RaiseCanExecuteChanged();
            _removeOneOrderItemCommand.RaiseCanExecuteChanged();
        }
    }

    public System.Windows.Input.ICommand RemoveOrderLineCommand =>
        _removeOrderLineCommand;
    public System.Windows.Input.ICommand RemoveOneOrderItemCommand =>
        _removeOneOrderItemCommand;
    private bool Matches(IItem item)
    {
        var textMatches =
            string.IsNullOrWhiteSpace(_filter)
            || item.DisplayName.Contains(_filter, StringComparison.OrdinalIgnoreCase)
            || (item is MenuItem menuItem
                && (menuItem.Category.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase)
                    || menuItem.Sku.Contains(_filter, StringComparison.OrdinalIgnoreCase)
                    || (menuItem.Origin == ItemOrigin.SupplierBought && "supplier bought".Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    || (menuItem.Origin == ItemOrigin.MadeInHouse && "made in house".Contains(_filter, StringComparison.OrdinalIgnoreCase))));

        var categoryMatches =
            _categoryFilter == "All categories"
            || item is MenuItem categorizedItem
            && string.Equals(
                categorizedItem.Category.Name,
                _categoryFilter,
                StringComparison.OrdinalIgnoreCase);

        var originMatches =
            _originFilter == "All"
            || (item is MenuItem originItem && (
                (_originFilter == "Made in House" && originItem.Origin == ItemOrigin.MadeInHouse) ||
                (_originFilter == "Supplier Bought" && originItem.Origin == ItemOrigin.SupplierBought)));

        return textMatches && categoryMatches && originMatches;
    }
    private void RemoveOneFromSelectedOrderLine()
    {
        if (SelectedOrderLine is not { } selected)
            return;

        var index = _orderLines.IndexOf(selected);
        if (index < 0 || !_order.RemoveOneFromLine(selected))
            return;

        var updatedLine = _order.Lines.FirstOrDefault(
            line => line.Item.Id == selected.Item.Id
                && line.Milk == selected.Milk
                && line.Syrup == selected.Syrup
                && line.Sugar == selected.Sugar
                && line.Bread == selected.Bread);

        if (updatedLine is null)
        {
            ReleaseReservedStock(selected.Item.Id, 1);
            _orderLines.RemoveAt(index);
            SelectedOrderLine = null;
        }
        else
        {
            ReleaseReservedStock(selected.Item.Id, 1);
            _orderLines[index] = updatedLine;
            SelectedOrderLine = updatedLine;
        }

        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(OrderVatTotal));
        OnPropertyChanged(nameof(AppliedDiscountPercent));
        OnPropertyChanged(nameof(ReceiptPreview));
        RefreshInventoryRows();
        ClearConvertedTotal();
        RaiseOrderCommandsCanExecuteChanged();
    }

    private void ApplyDiscount()
    {
        if (!TryGetDiscountInput(out var discountPercent))
            return;

        _order.ApplyDiscount(discountPercent);
        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(OrderVatTotal));
        OnPropertyChanged(nameof(AppliedDiscountPercent));
        OnPropertyChanged(nameof(ReceiptPreview));
        ClearConvertedTotal();
    }

    private bool TryGetDiscountInput(out decimal discountPercent) =>
        decimal.TryParse(
            DiscountInput,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out discountPercent)
        && discountPercent is >= 0 and <= 100;

    private async Task ConvertOrderAsync()
    {
        var converted = await _order.ConvertTotalAsync(_currencyConverter, TargetCurrency);
        _convertedOrderTotal = converted;
        OnPropertyChanged(nameof(ConvertedOrderDisplay));
        OperationMessage = $"Order total converted to {TargetCurrency}.";
    }

    private async Task CheckoutAsync()
    {
        var checkout = new CheckoutService(
            _currencyConverter,
            new DemoPaymentProcessor(),
            new CompletedOrderHistory());
        var result = await checkout.CheckoutAsync(_order, PaymentCurrency);

        if (!result.IsApproved)
        {
            OperationMessage =
                $"Payment declined for {result.PaymentAmount.Amount:0.00} {result.PaymentAmount.Currency}.";
            return;
        }

        _lastCompletedReceipt =
            $"{CafeReportFormatter.FormatReceipt(_order)}Paid: {result.PaymentAmount.Amount:0.00} {result.PaymentAmount.Currency}{Environment.NewLine}";
        await CommitOrderStockAsync();
        var history = await CompletedOrderHistoryFile.LoadAsync(_completedOrdersPath, 10000);
        history.Record(_order);
        await CompletedOrderHistoryFile.SaveAsync(history, _completedOrdersPath);

        _order = CreateNewOrder();
        _currentOrderStockCommitted = false;
        _orderLines.Clear();
        SelectedOrderLine = null;
        DiscountInput = "0";
        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(OrderVatTotal));
        OnPropertyChanged(nameof(AppliedDiscountPercent));
        OnPropertyChanged(nameof(ReceiptPreview));
        ClearConvertedTotal();
        RaiseOrderCommandsCanExecuteChanged();
        OperationMessage =
            $"Demo payment approved ({SelectedPaymentMethod}, {PaymentCurrency}): {result.PaymentAmount.Amount:0.00} {result.PaymentAmount.Currency}. No real payment was taken.";
    }

    private async Task CompleteOrderAsync()
    {
        var history = await CompletedOrderHistoryFile.LoadAsync(_completedOrdersPath, 10000);
        var hasOpenOrder = _orderLines.Count > 0;
        if (hasOpenOrder)
        {
            await CommitOrderStockAsync();
            history.Record(_order);
        }

        var report = CafeReportFormatter.FormatEndOfDay(
            history.Orders,
            DateOnly.FromDateTime(DateTime.Now));
        var reportPath = EndOfDayExportPath;
        var reportBytes = System.Text.Encoding.UTF8.GetBytes(report);

        await AtomicFile.WriteAsync(
            reportPath,
            (stream, token) => stream.WriteAsync(reportBytes, token).AsTask(),
            CancellationToken.None);
        await CompletedOrderHistoryFile.SaveAsync(history, _completedOrdersPath);

        if (hasOpenOrder)
        {
            _order = CreateNewOrder();
            _currentOrderStockCommitted = false;
            _orderLines.Clear();
            SelectedOrderLine = null;
            DiscountInput = "0";
            OnPropertyChanged(nameof(OrderTotal));
            OnPropertyChanged(nameof(OrderVatTotal));
            OnPropertyChanged(nameof(AppliedDiscountPercent));
            OnPropertyChanged(nameof(ReceiptPreview));
            ClearConvertedTotal();
        }

        RaiseOrderCommandsCanExecuteChanged();
        OperationMessage =
            $"Day saved and exported ({history.Count} saved orders). Orders: {_completedOrdersPath}. Report: {reportPath}";
    }

    private void ClearConvertedTotal()
    {
        if (_convertedOrderTotal is null)
            return;

        _convertedOrderTotal = null;
        OnPropertyChanged(nameof(ConvertedOrderDisplay));
    }

    private void RaiseOrderCommandsCanExecuteChanged()
    {
        _applyDiscountCommand.RaiseCanExecuteChanged();
        _convertOrderCommand.RaiseCanExecuteChanged();
        _completeOrderCommand.RaiseCanExecuteChanged();
        _checkoutCommand.RaiseCanExecuteChanged();
        _saveStartingStockCommand.RaiseCanExecuteChanged();
    }

    private int AvailableStock(MenuItem item) =>
        _inventoryLevels.TryGetValue(item.Id, out var level)
            ? Math.Max(0, level.RemainingQuantity - _reservedQuantities.GetValueOrDefault(item.Id))
            : 0;

    private static string GetStockStatus(int remaining) =>
        remaining switch
        {
            0 => "Sold out for today",
            <= 2 => $"Almost sold out — only {remaining} left",
            <= 5 => $"Low stock — only {remaining} left",
            _ => $"{remaining} pieces left"
        };

    private void ReleaseReservedStock(string itemId, int quantity)
    {
        var reserved = _reservedQuantities.GetValueOrDefault(itemId) - quantity;
        if (reserved <= 0)
            _reservedQuantities.Remove(itemId);
        else
            _reservedQuantities[itemId] = reserved;
    }

    private void RefreshInventoryRows()
    {
        foreach (var row in _inventoryItems)
        {
            if (!_inventoryLevels.TryGetValue(row.Item.Id, out var level))
                continue;
            row.UpdateStock(AvailableStock(row.Item), level.StartingQuantity);
        }

        OnPropertyChanged(nameof(DetailsStockStatus));
    }

    private async Task SetStartingStockAsync(object? parameter)
    {
        if (parameter is not InventoryItemRow row)
            return;
        if (!row.HasValidStartingQuantity)
        {
            OperationMessage = "Enter a non-negative whole number for starting stock.";
            return;
        }
        if (_orderLines.Any(line => line.Item.Id == row.Item.Id))
        {
            OperationMessage = $"Finish or remove {row.Item.DisplayName} from the current order before changing its stock.";
            return;
        }

        var quantity = row.ParsedStartingQuantity;
        var next = new InventoryStockLevel(quantity, quantity);
        var updated = new Dictionary<string, InventoryStockLevel>(_inventoryLevels, StringComparer.Ordinal)
        {
            [row.Item.Id] = next
        };
        await InventoryFile.SaveAsync(_inventoryPath, _businessDate, updated);
        _inventoryLevels[row.Item.Id] = next;
        _store.SetAvailability(row.Item.Id, quantity > 0);
        RefreshInventoryRows();
        RefreshMenuSections();
        RaiseOrderCommandsCanExecuteChanged();
        OperationMessage = $"Starting stock for {row.Item.DisplayName} set to {quantity}.";
    }

    private async Task CommitOrderStockAsync()
    {
        if (_orderLines.Count == 0 || _currentOrderStockCommitted)
            return;

        var updated = new Dictionary<string, InventoryStockLevel>(_inventoryLevels, StringComparer.Ordinal);
        foreach (var line in _orderLines)
        {
            var level = updated[line.Item.Id];
            if (level.RemainingQuantity < line.Quantity)
                throw new InvalidOperationException(
                    $"Not enough {line.Item.DisplayName} remains in inventory to complete this order.");

            var remaining = level.RemainingQuantity - line.Quantity;
            updated[line.Item.Id] = level with
            {
                RemainingQuantity = remaining
            };
            if (remaining == 0)
                _store.SetAvailability(line.Item.Id, false);
        }

        await InventoryFile.SaveAsync(_inventoryPath, _businessDate, updated);
        foreach (var pair in updated)
            _inventoryLevels[pair.Key] = pair.Value;
        _reservedQuantities.Clear();
        _currentOrderStockCommitted = true;
        RefreshInventoryRows();
        RefreshMenuSections();
    }

    private Order CreateNewOrder(DateTime? placedAt = null)
    {
        var order = new Order("RON", placedAt ?? _currentTime);
        order.SetServiceDetails(
            ParseServiceMode(_selectedServiceMode),
            ParsePaymentMethod(_selectedPaymentMethod));
        return order;
    }

    private static ServiceMode ParseServiceMode(string value) =>
        value switch
        {
            "Dine-in" => ServiceMode.DineIn,
            "Takeaway" => ServiceMode.Takeaway,
            _ => throw new ArgumentException("Choose a supported service mode.", nameof(value))
        };

    private static PaymentMethod ParsePaymentMethod(string value) =>
        value switch
        {
            "Cash" => PaymentMethod.Cash,
            "Card" => PaymentMethod.Card,
            "Mobile wallet" => PaymentMethod.MobileWallet,
            _ => throw new ArgumentException("Choose a supported payment method.", nameof(value))
        };

    private void NotifyCountIfChanged(int previousCount)
    {
        if (previousCount != Count)
            OnPropertyChanged(nameof(Count));
    }
    private void RefreshRecentlyUsedItems()
    {
        _recentItems.Clear();

        foreach (var item in _recentlyUsed.Items())
            _recentItems.Add(item);

        RefreshMostOrderedItems();
    }

    private void RefreshMostOrderedItems()
    {
        _mostOrderedItems.Clear();
        var menuItems = _store.All().OfType<MenuItem>().ToArray();
        foreach (var item in menuItems
                     .Where(item => item.IsAvailable && _orderCounts.ContainsKey(item.Id))
                     .OrderByDescending(item => _orderCounts[item.Id])
                     .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                     .Take(5))
        {
            _mostOrderedItems.Add(item);
        }
    }

    private void RefreshMenuSections()
    {
        _menuSections.Clear();

        var menuItems = _items.OfType<MenuItem>().ToList();
        var availableItems = menuItems.Where(item => item.IsAvailable && AvailableStock(item) > 0).ToList();
        var soldOutItems = menuItems.Where(item => !item.IsAvailable || AvailableStock(item) <= 0).ToList();

        foreach (var categoryGroup in availableItems
                     .GroupBy(item => item.Category.Name, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var section = new MenuCategorySection(categoryGroup.Key, isSoldOutSection: false);
            foreach (var item in categoryGroup.OrderBy(
                         item => item.DisplayName,
                         StringComparer.OrdinalIgnoreCase))
            {
                section.Items.Add(item);
            }

            _menuSections.Add(section);
        }

        if (soldOutItems.Count > 0)
        {
            var soldOutSection = new MenuCategorySection("Sold Out", isSoldOutSection: true);
            foreach (var item in soldOutItems.OrderBy(
                         item => item.DisplayName,
                         StringComparer.OrdinalIgnoreCase))
            {
                soldOutSection.Items.Add(item);
            }

            _menuSections.Add(soldOutSection);
        }
    }

    private void OnItemAvailabilityChanged(object? sender, AvailabilityChangedChange e)
    {
        Refill();
    }

    private void OnItemUpdated(object? sender, ItemUpdatedChange e)
    {
        Refill();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}