using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Capstone.Core.ViewModels;
using CafeMenuItem = Capstone.Core.Models.MenuItem;

namespace Capstone.Ui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        clockTimer.Tick += (_, _) =>
        {
            if (DataContext is CatalogueViewModel viewModel)
                viewModel.UpdateClock(DateTime.Now);
        };
        clockTimer.Start();
        Closed += (_, _) => clockTimer.Stop();
    }

    private void AddMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CafeMenuItem item }
            && DataContext is CatalogueViewModel viewModel)
        {
            viewModel.AddItemToOrder(item);
        }
    }

    private void SelectMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CafeMenuItem item }
            && DataContext is CatalogueViewModel viewModel)
        {
            viewModel.SelectMenuItemCommand.Execute(item);
        }
    }

    private void SelectCategory_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: string category }
            && DataContext is CatalogueViewModel viewModel)
        {
            viewModel.SelectCategoryCommand.Execute(category);
        }
    }

    private void SaveStartingStock_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: InventoryItemRow row }
            && DataContext is CatalogueViewModel viewModel)
        {
            viewModel.SaveStartingStockCommand.Execute(row);
        }
    }
}