using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

/// <summary>
/// Connects the menu item domain objects to the Lab 02 contract tests.
/// </summary>
public sealed class Lab02Wiring : ILab02Wiring
{
    public IItemStore CreateStore() => new CafeStore();

    public IItemMapper CreateMapper() => new MenuItemMapper();

    public IItem CreateSample(int seed) =>
        new MenuItem(
            $"item-{seed}",
            $"Menu Item {seed}",
            new Category("cof", "Coffee"),
            new Money(10m, "RON"));
}