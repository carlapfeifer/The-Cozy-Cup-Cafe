namespace Capstone.Tests;
using Capstone.Core;
using Capstone.Core.Models;
public class SmokeTests
{
    /*
     * Store duplicate test:
     *
     * Arrange:
     * Create a CafeStore and add a Drink with ID "c1".
     *
     * Act:
     * Add a second Drink with the same ID "c1".
     *
     * Assert:
     * AssertThrows verifies the duplicate is rejected and Count remains 1.
     *
     * A store bug that overwrites or permits duplicate IDs would fail this test.
     */
    [Fact]
    public void Duplicate_store_ids_are_rejected()
    {
        var store = new CafeStore();
        var coffee = new Category("cof", "Coffee");

        store.Add(new Drink(
            "c1",
            "Latte",
            coffee,
            new Money(10m, "RON")));

        Assert.Throws<ArgumentException>(() =>
            store.Add(new Drink(
                "c1",
                "Mocha",
                coffee,
                new Money(12m, "RON"))));

        Assert.Equal(1, store.Count);
    }
}