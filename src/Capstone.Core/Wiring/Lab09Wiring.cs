using Capstone.Core.ViewModels;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab09Wiring : ILab09Wiring
{
    public IItemStore CreateStore() => new CafeStore();

    public IItem CreateSample(int seed) =>
        new Lab02Wiring().CreateSample(seed);

    public ICatalogueViewModel CreateViewModel(IItemStore store) =>
        new CatalogueViewModel(store);
}