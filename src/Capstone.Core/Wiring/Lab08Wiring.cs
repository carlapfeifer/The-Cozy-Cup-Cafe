using Capstone.Core.Storage;
using SharpQuest.Contracts;
using System.IO;
namespace Capstone.Core.Wiring;

public sealed class Lab08Wiring : ILab08Wiring
{
    private readonly Lab02Wiring _lab02 = new();
    private readonly Lab04Wiring _lab04 = new();

    public IItemStore CreateStore() => new CafeStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public IReadOnlyList<Type> KindTypes => _lab04.KindTypes;

    public IItem CreateOfKind(int kindIndex, int seed) => _lab04.CreateOfKind(kindIndex, seed);

    public Task SaveAsync(IItemStore store, string path) =>
        SaveAsync(store, path, CancellationToken.None);

    public async Task SaveAsync(
        IItemStore store,
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(path).ToLowerInvariant();
        switch (extension)
        {
            case ".json":
                await MenuFile.SaveAsync(store, path, cancellationToken);
                break;
            case ".csv":
                await new CsvMenuStorage().SaveAsync(store, path, cancellationToken);
                break;
            default:
                throw new ArgumentException(
                    "Menu storage supports only .json and .csv files.",
                    nameof(path));
        }
    }

    public Task<IItemStore> LoadAsync(string path) =>
        LoadAsync(path, CancellationToken.None);

    public async Task<IItemStore> LoadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".json" => await MenuFile.LoadAsync(path, cancellationToken),
            ".csv" => await new CsvMenuStorage().LoadAsync(path, cancellationToken),
            _ => throw new ArgumentException(
                "Menu storage supports only .json and .csv files.",
                nameof(path))
        };
    }
}