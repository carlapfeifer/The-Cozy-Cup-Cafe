namespace Capstone.Core.Storage;

internal static class AtomicFile
{
    public static async Task WriteAsync(
        string path,
        Func<FileStream, CancellationToken, Task> writeContent,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(writeContent);
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var temporaryPath = path + ".tmp";

        await using (var stream = new FileStream(
                         temporaryPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 4096,
                         useAsync: true))
        {
            await writeContent(stream, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(path))
            File.Copy(path, path + ".bak", overwrite: true);

        File.Move(temporaryPath, path, overwrite: true);
    }
}