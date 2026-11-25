using SharpQuest.Contracts;

namespace Capstone.Core.Storage;

public sealed class JsonMenuStorage : IMenuStorage
{
    public Task SaveAsync(
        IItemStore store,
        string path,
        CancellationToken cancellationToken = default) =>
        MenuFile.SaveAsync(store, path, cancellationToken);

    public Task<IItemStore> LoadAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        MenuFile.LoadAsync(path, cancellationToken);
}