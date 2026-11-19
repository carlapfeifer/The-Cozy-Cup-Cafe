using Capstone.Core;
using Capstone.Core.Models;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Tests;

public class MenuFileTests : IDisposable
{
    private static readonly Category Coffee = new("cof", "Coffee");
    private static readonly Category Pastries = new("pas", "Pastries");

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "cozycup-" + Guid.NewGuid().ToString("N"));
    private string PathFor(string name) => Path.Combine(_folder, name);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static CafeStore SampleMenu()
    {
        var store = new CafeStore();
        var latte = new Drink("c1", "Latte", Coffee, new Money(14.5m, "RON"), Size.Large, Allergen.Milk);
        var croissant = new Food("p1", "Croissant", Pastries, new Money(8m, "RON"), Allergen.Gluten | Allergen.Eggs);
        store.Add(latte);
        store.Add(new Drink("c2", "Mocha", Coffee, new Money(13m, "RON"), Size.Small, Allergen.None, isAvailable: false));
        store.Add(croissant);
        store.Add(new Combo("m1", "Breakfast", Coffee, new Money(19m, "RON"), new MenuItem[] { latte, croissant }));
        return store;
    }

    [Fact]
    public async Task A_drink_comes_back_with_every_field()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path);

        var back = (Drink)(await MenuFile.LoadAsync(path)).FindById("c1")!;

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
        await MenuFile.SaveAsync(SampleMenu(), path);

        var back = (Drink)(await MenuFile.LoadAsync(path)).FindById("c2")!;

        Assert.False(back.IsAvailable);
        Assert.Equal(Size.Small, back.Size);
    }

    [Fact]
    public async Task Food_comes_back_as_food_with_all_its_allergens()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path);

        var back = (await MenuFile.LoadAsync(path)).FindById("p1");

        var food = Assert.IsType<Food>(back);
        Assert.Equal(Allergen.Gluten | Allergen.Eggs, food.Allergens);
        Assert.Equal(Pastries, food.Category);
    }

    [Fact]
    public async Task A_combo_comes_back_as_a_combo_with_its_parts()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path);

        var back = (await MenuFile.LoadAsync(path)).FindById("m1");

        var combo = Assert.IsType<Combo>(back);
        Assert.Equal(new[] { "c1", "p1" }, combo.Parts.Select(p => p.Id).ToArray());
        Assert.Equal(Allergen.Milk | Allergen.Gluten | Allergen.Eggs, combo.Allergens);
        Assert.Equal(new Money(19m, "RON"), combo.Price);
    }

    [Fact]
    public async Task Items_come_back_in_the_same_order()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path);

        var ids = (await MenuFile.LoadAsync(path)).All().Select(i => i.Id).ToArray();

        Assert.Equal(new[] { "c1", "c2", "p1", "m1" }, ids);
    }

    [Fact]
    public async Task Saving_creates_a_folder_that_does_not_exist_yet()
    {
        var path = Path.Combine(_folder, "deep", "er", "menu.json");

        await MenuFile.SaveAsync(SampleMenu(), path);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task A_save_is_a_snapshot()
    {
        var path = PathFor("menu.json");
        var store = SampleMenu();
        await MenuFile.SaveAsync(store, path);

        store.Add(new Food("p2", "Tart", Pastries, new Money(9m, "RON")));

        Assert.Equal(4, (await MenuFile.LoadAsync(path)).Count);
    }

    [Fact]
    public async Task A_file_that_is_not_there_is_an_empty_store()
    {
        var store = await MenuFile.LoadAsync(PathFor("nothing-yet.json"));

        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task A_folder_that_is_not_there_is_an_empty_store_too()
    {
        var store = await MenuFile.LoadAsync(Path.Combine(_folder, "no", "such", "folder", "menu.json"));

        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task A_file_full_of_nonsense_is_an_error_not_an_empty_store()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        await File.WriteAllTextAsync(path, "this is not a menu {{{");

        await Assert.ThrowsAsync<InvalidDataException>(() => MenuFile.LoadAsync(path));
    }

    [Fact]
    public async Task Half_a_file_is_an_error()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path);
        var text = await File.ReadAllTextAsync(path);
        await File.WriteAllTextAsync(path, text[..(text.Length / 2)]);

        await Assert.ThrowsAsync<InvalidDataException>(() => MenuFile.LoadAsync(path));
    }

    [Fact]
    public async Task An_empty_file_is_an_error()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        await File.WriteAllTextAsync(path, "");

        await Assert.ThrowsAsync<InvalidDataException>(() => MenuFile.LoadAsync(path));
    }

    [Fact]
    public async Task An_unknown_kind_is_an_error()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        await File.WriteAllTextAsync(path,
            """[{"kind":"pizza","id":"x","displayName":"X","categoryId":"c","categoryName":"C","amount":1,"currency":"RON"}]""");

        await Assert.ThrowsAsync<InvalidDataException>(() => MenuFile.LoadAsync(path));
    }

    [Fact]
    public async Task Two_rows_with_the_same_id_are_an_error()
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor("menu.json");
        const string row = """{"kind":"food","id":"x","displayName":"X","categoryId":"c","categoryName":"C","amount":1,"currency":"RON"}""";
        await File.WriteAllTextAsync(path, $"[{row},{row}]");

        await Assert.ThrowsAsync<InvalidDataException>(() => MenuFile.LoadAsync(path));
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_programming_errors()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => MenuFile.SaveAsync(null!, PathFor("menu.json")));
        await Assert.ThrowsAsync<ArgumentException>(() => MenuFile.SaveAsync(new CafeStore(), "  "));
        await Assert.ThrowsAsync<ArgumentException>(() => MenuFile.LoadAsync("  "));
    }
    
    
    [Fact]
    public async Task Saving_again_replaces_the_old_file_and_leaves_no_temporary_file()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path);

        var smaller = new CafeStore();
        smaller.Add(new Food("p9", "Bun", Pastries, new Money(5m, "RON")));
        await MenuFile.SaveAsync(smaller, path);

        Assert.Equal(1, (await MenuFile.LoadAsync(path)).Count);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public async Task A_crash_halfway_through_a_save_leaves_the_old_file_intact()
    {
        var path = PathFor("menu.json");
        await MenuFile.SaveAsync(SampleMenu(), path);

        // what a save that crashed halfway would leave behind
        await File.WriteAllTextAsync(path + ".tmp", "[{\"kind\":\"drink\",\"id\":\"half-writ");

        Assert.Equal(4, (await MenuFile.LoadAsync(path)).Count);

        await MenuFile.SaveAsync(SampleMenu(), path);   // the next save simply overwrites the leftover
        Assert.False(File.Exists(path + ".tmp"));
    }
}