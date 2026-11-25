using SharpQuest.Contracts;

namespace Capstone.Core.Storage;

public interface IMenuStorage
{
    Task SaveAsync(
        IItemStore store,
        string path,
        CancellationToken cancellationToken = default);

    Task<IItemStore> LoadAsync(
        string path,
        CancellationToken cancellationToken = default);
}