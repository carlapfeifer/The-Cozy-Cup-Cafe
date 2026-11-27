using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Capstone.Core.Wiring;
using System.Text.Json;
using Capstone.Core.Models;
using Capstone.Core.ViewModels;

namespace Capstone.Ui;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var wiring = new Lab09Wiring();
            var store = wiring.CreateStore();

            var menuPath = Path.Combine(AppContext.BaseDirectory, "menu.json");
            using var document = JsonDocument.Parse(File.ReadAllText(menuPath));

            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("The menu file must contain a JSON array.");

            var mapper = new MenuItemMapper();
            var entryIndex = 0;

            foreach (var entry in document.RootElement.EnumerateArray())
            {
                var item = mapper.FromJson(entry.GetRawText())
                           ?? throw new InvalidDataException($"Invalid menu entry at index {entryIndex}.");

                store.Add(item);
                entryIndex++;
            }

            var completedOrdersPath = Path.Combine(AppContext.BaseDirectory, "completed-orders.json");
            var reportsDir = Path.Combine(AppContext.BaseDirectory, "Reports");
            var inventoryPath = Path.Combine(AppContext.BaseDirectory, "inventory.json");

            desktop.MainWindow = new MainWindow
            {
                DataContext = new CatalogueViewModel(
                    store,
                    exchangeRateProvider: null,
                    completedOrdersPath: completedOrdersPath,
                    reportsDirectory: reportsDir,
                    inventoryPath: inventoryPath)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}