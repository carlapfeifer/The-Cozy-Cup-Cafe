using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Storage;
using Capstone.Core.Wiring;
namespace Capstone.Tests;

public class MenuFileTests : IDisposable
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Pastries = new("pas", "Pastries");

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "cozycup-" + Guid.NewGuid().ToString("N"));
    
    

    private string PathFor(string name) => Path.Combine(_folder, name);

    private static CafeStore SampleMenu()
    {
        var store = new CafeStore();
        var latte = new Drink(
            "c1", "Latte", Coffee, new Money(14.5m, "RON"), Size.Large, Allergen.Milk);
        var croissant = new Food(
            "p1", "Croissant", Pastries, new Money(8m, "RON"),
            Allergen.Gluten | Allergen.Eggs);

        store.Add(latte);
        store.Add(new Drink(
            "c2", "Mocha", Coffee, new Money(13m, "RON"),
            Size.Small, Allergen.None, isAvailable: false));
        store.Add(croissant);
        store.Add(new Combo(
            "m1", "Breakfast", Coffee, new Money(19m, "RON"),
            new MenuItem[] { latte, croissant }));

        return store;
    }

    private static CafeStore ExtendedSampleMenu()
    {
        var store = new CafeStore();
        var coffee = new Category("coffee", "Coffee", 12m, "cup", "#123456");
        var pastry = new Category("pastry", "Pastries");

        var latte = new Drink(
            "latte", "Latte", coffee, new Money(14m, "RON"),
            Size.Large, Allergen.Milk, isAvailable: true, isFeatured: true,
            isHot: false, isDecaf: true);
        var croissant = new Food(
            "croissant", "Croissant", pastry, new Money(8m, "RON"),
            Allergen.Gluten | Allergen.Eggs, isAvailable: false);

        store.Add(latte);
        store.Add(croissant);
        store.Add(new Combo(
            "breakfast", "Breakfast", pastry, new Money(20m, "RON"),
            new MenuItem[] { latte, croissant }));

        return store;
    }

    

    [Fact]
    public async Task A_drink_comes_back_with_every_field()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        var back = (Drink)(await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).FindById("c1")!;

        Assert.Equal("Latte", back.DisplayName);
        Assert.Equal(Coffee, back.Category);
        Assert.Equal(new Money(14.5m, "RON"), back.Price);
        Assert.Equal(Size.Large, back.Size);
        Assert.Equal(Allergen.Milk, back.Allergens);
        Assert.True(back.IsAvailable);
    }

    [Fact]
    public async Task A_sold_out_drink_stays_sold_out()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        var back = (Drink)(await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).FindById("c2")!;

        Assert.False(back.IsAvailable);
        Assert.Equal(Size.Small, back.Size);
    }

    [Fact]
    public async Task Food_comes_back_as_food_with_all_its_allergens()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        var back = (await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).FindById("p1");

        var food = Assert.IsType<Food>(back);
        Assert.Equal(Allergen.Gluten | Allergen.Eggs, food.Allergens);
        Assert.Equal(Pastries, food.Category);
    }

    [Fact]
    public async Task A_combo_comes_back_as_a_combo_with_its_parts()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        var back = (await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).FindById("m1");

        var combo = Assert.IsType<Combo>(back);
        Assert.Equal(new[] { "c1", "p1" }, combo.Parts.Select(p => p.Id).ToArray());
        Assert.Equal(Allergen.Milk | Allergen.Gluten | Allergen.Eggs, combo.Allergens);
        Assert.Equal(new Money(19m, "RON"), combo.Price);
    }

    [Fact]
    public async Task Items_come_back_in_the_same_order()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        var ids = (await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).All().Select(i => i.Id).ToArray();

        Assert.Equal(new[] { "c1", "c2", "p1", "m1" }, ids);
    }

    [Fact]
    public async Task Saving_creates_a_folder_that_does_not_exist_yet()
    {
        var path = Path.Combine(_folder, "deep", "er", "menu.json");

        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task A_save_is_a_snapshot()
    {
        var path = PathFor("menu.json");
        var store = SampleMenu();
        await MenuFile.SaveAsync(store, path, TestContext.Current.CancellationToken);

        store.Add(new Food("p2", "Tart", Pastries, new Money(9m, "RON")));

        Assert.Equal(4, (await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).Count);
    }

    [Fact]
    public async Task A_file_that_is_not_there_is_an_empty_store()
    {
        var store = await MenuFile.LoadAsync(
            PathFor("nothing-yet.json"), TestContext.Current.CancellationToken);

        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task A_folder_that_is_not_there_is_an_empty_store_too()
    {
        var store = await MenuFile.LoadAsync(
            Path.Combine(_folder, "no", "such", "folder", "menu.json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task A_file_full_of_nonsense_is_an_error_not_an_empty_store()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        await File.WriteAllTextAsync(
            path, "this is not a menu {{{", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MenuFile.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Half_a_file_is_an_error()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);
        var text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            path, text[..(text.Length / 2)], TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MenuFile.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_empty_file_is_an_error()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        await File.WriteAllTextAsync(path, "", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MenuFile.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_unknown_kind_is_an_error()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        await File.WriteAllTextAsync(
            path,
            """[{"kind":"pizza","id":"x","displayName":"X","categoryId":"c","categoryName":"C","amount":1,"currency":"RON"}]""",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MenuFile.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Two_rows_with_the_same_id_are_an_error()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        const string row = """{"kind":"food","id":"x","displayName":"X","categoryId":"c","categoryName":"C","amount":1,"currency":"RON"}""";
        await File.WriteAllTextAsync(
            path, $"[{row},{row}]", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MenuFile.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_programming_errors()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => MenuFile.SaveAsync(null!, PathFor("menu.json"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(
            () => MenuFile.SaveAsync(new CafeStore(), "  ", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(
            () => MenuFile.LoadAsync("  ", TestContext.Current.CancellationToken));
    }
    
    
    [Fact]
    public async Task Saving_again_replaces_the_old_file_and_leaves_no_temporary_file()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        var smaller = new CafeStore();
        smaller.Add(new Food("p9", "Bun", Pastries, new Money(5m, "RON")));
        await MenuFile.SaveAsync(smaller, path, TestContext.Current.CancellationToken);

        Assert.Equal(1, (await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).Count);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public async Task A_crash_halfway_through_a_save_leaves_the_old_file_intact()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);

        // what a save that crashed halfway would leave behind
        await File.WriteAllTextAsync(
            path + ".tmp",
            "[{\"kind\":\"drink\",\"id\":\"half-writ",
            TestContext.Current.CancellationToken);

        Assert.Equal(4, (await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken)).Count);

        await MenuFile.SaveAsync(SampleMenu(), path, TestContext.Current.CancellationToken);   // the next save simply overwrites the leftover
        Assert.False(File.Exists(path + ".tmp"));
    }
    [Fact]
    public async Task Save_load_preserves_featured_state()
    {
        var store = new CafeStore();
        var coffee = new Category("cof", "Coffee");

        store.Add(new Drink(
            "featured-1",
            "Featured Latte",
            coffee,
            new Money(14m, "RON"),
            Size.Medium,
            Allergen.Milk,
            true,
            true));

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.json");

        await MenuFile.SaveAsync(store, path, TestContext.Current.CancellationToken);

        try
        {
            var loaded = await MenuFile.LoadAsync(path, TestContext.Current.CancellationToken);
            var item = Assert.IsType<Drink>(Assert.Single(loaded.All()));

            Assert.True(item.IsFeatured);
        }
        finally
        {
            File.Delete(path);
        }
    }
    
    [Fact]
    public async Task Csv_round_trip_preserves_item_kinds_fields_order_and_combo_availability()
    {
        var path = PathFor("menu.csv");
        var storage = new CsvMenuStorage();

        await storage.SaveAsync(ExtendedSampleMenu(), path, TestContext.Current.CancellationToken);
        var loaded = await storage.LoadAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { "latte", "croissant", "breakfast" },
            loaded.All().Select(item => item.Id));

        var drink = Assert.IsType<Drink>(loaded.FindById("latte"));
        Assert.Equal(Size.Large, drink.Size);
        Assert.Equal(Allergen.Milk, drink.Allergens);
        Assert.True(drink.IsFeatured);
        Assert.False(drink.IsHot);
        Assert.True(drink.IsDecaf);
        Assert.Equal(12m, drink.Category.VatPercent);
        Assert.Equal("#123456", drink.Category.DisplayColor);

        var food = Assert.IsType<Food>(loaded.FindById("croissant"));
        Assert.False(food.IsAvailable);

        var combo = Assert.IsType<Combo>(loaded.FindById("breakfast"));
        Assert.Equal(new[] { "latte", "croissant" }, combo.Parts.Select(part => part.Id));
        Assert.False(combo.IsAvailable);
    }

    [Fact]
    public async Task Wiring_selects_json_and_csv_by_extension()
    {
        var wiring = new Lab08Wiring();
        var store = ExtendedSampleMenu();

        await wiring.SaveAsync(store, PathFor("menu.csv"), TestContext.Current.CancellationToken);
        await wiring.SaveAsync(store, PathFor("menu.json"), TestContext.Current.CancellationToken);

        Assert.Equal(3, (await wiring.LoadAsync(
            PathFor("menu.csv"), TestContext.Current.CancellationToken)).Count);
        Assert.Equal(3, (await wiring.LoadAsync(
            PathFor("menu.json"), TestContext.Current.CancellationToken)).Count);
    }

    [Fact]
    public async Task Wiring_rejects_unsupported_extensions()
    {
        var wiring = new Lab08Wiring();

        await Assert.ThrowsAsync<ArgumentException>(
            () => wiring.LoadAsync(PathFor("menu.xml"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(
            () => wiring.SaveAsync(
                SampleMenu(), PathFor("menu.txt"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Json_save_creates_a_backup_of_the_previous_file()
    {
        var path = PathFor("menu.json");
        var storage = new JsonMenuStorage();

        await storage.SaveAsync(ExtendedSampleMenu(), path, TestContext.Current.CancellationToken);
        await storage.SaveAsync(new CafeStore(), path, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(path + ".bak"));
        Assert.Equal(3, (await storage.LoadAsync(
            path + ".bak", TestContext.Current.CancellationToken)).Count);
        Assert.Empty((await storage.LoadAsync(
            path, TestContext.Current.CancellationToken)).All());
    }

    [Fact]
    public async Task Json_rejects_an_unsupported_format_version()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        await File.WriteAllTextAsync(
            path,
            """{"formatVersion":999,"savedAtUtc":"2026-01-01T00:00:00Z","items":[]}""",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MenuFile.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Save_and_load_honor_cancellation()
    {
        var storage = new JsonMenuStorage();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => storage.SaveAsync(SampleMenu(), PathFor("cancelled.json"), source.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => storage.LoadAsync(PathFor("anything.json"), source.Token));
    }

    [Theory]
    [InlineData("""[{"kind":"food"}]""")]
    [InlineData("""[{"kind":"drink","id":"x","displayName":"X","categoryId":"c","categoryName":"C","amount":1,"currency":"RON","size":"Jumbo"}]""")]
    [InlineData("""[{"kind":"food","id":"x","displayName":"X","categoryId":"c","categoryName":"C","amount":-1,"currency":"RON"}]""")]
    [InlineData("""[{"kind":"combo","id":"x","displayName":"X","categoryId":"c","categoryName":"C","amount":1,"currency":"RON","parts":[]}]""")]
    public async Task Json_rejects_missing_fields_unknown_enums_negative_prices_and_empty_combos(
        string json)
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("invalid.json");
        await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MenuFile.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Completed_order_history_round_trips_to_a_separate_file()
    {
        var history = new CompletedOrderHistory(capacity: 3);
        var item = Assert.IsType<Drink>(ExtendedSampleMenu().FindById("latte"));
        var order = new Order(placedAt: new DateTime(2026, 11, 25, 12, 0, 0));
        order.Add(item, 2);
        order.ApplyDiscount(10m);
        history.Record(order);

        var path = PathFor("orders.json");
        await CompletedOrderHistoryFile.SaveAsync(
            history, path, TestContext.Current.CancellationToken);
        var loaded = await CompletedOrderHistoryFile.LoadAsync(
            path, TestContext.Current.CancellationToken);

        Assert.Equal(1, loaded.Count);
        Assert.Equal(3, loaded.Capacity);
        Assert.Equal(10m, Assert.Single(loaded.Orders).DiscountPercent);
        Assert.Equal(2, Assert.Single(loaded.Orders[0].Lines).Quantity);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);

        GC.SuppressFinalize(this);
    }

}