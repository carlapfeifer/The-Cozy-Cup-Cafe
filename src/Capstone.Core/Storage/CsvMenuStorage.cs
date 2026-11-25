using System.Globalization;
using System.Text;
using System.Text.Json;
using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Storage;

public sealed class CsvMenuStorage : IMenuStorage
{
    private const string Header = "formatVersion,savedAtUtc,itemJson";

    public async Task SaveAsync(
        IItemStore store,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        var savedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var compactOptions = new JsonSerializerOptions(MenuFile.SerializerOptions)
        {
            WriteIndented = false
        };

        var content = new StringBuilder();
        content.AppendLine(Header);

        foreach (var item in store.All())
        {
            var json = JsonSerializer.Serialize(MenuFile.ToRow(item), compactOptions);
            content.Append(CsvField(MenuFile.CurrentFormatVersion.ToString(CultureInfo.InvariantCulture)))
                .Append(',')
                .Append(CsvField(savedAt))
                .Append(',')
                .Append(CsvField(json))
                .AppendLine();
        }

        var bytes = Encoding.UTF8.GetBytes(content.ToString());

        await AtomicFile.WriteAsync(
            path,
            (stream, token) => stream.WriteAsync(bytes, token).AsTask(),
            cancellationToken);
    }

    public async Task<IItemStore> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        string[] lines;

        try
        {
            lines = await File.ReadAllLinesAsync(path, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return new CafeStore();
        }
        catch (DirectoryNotFoundException)
        {
            return new CafeStore();
        }

        if (lines.Length == 0 || lines[0] != Header)
            throw new InvalidDataException("The CSV menu file has an invalid header.");

        var store = new CafeStore();

        try
        {
            for (var index = 1; index < lines.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(lines[index]))
                    continue;

                var fields = ParseCsvLine(lines[index]);

                if (fields.Count != 3)
                    throw new InvalidDataException($"CSV row {index + 1} must have three fields.");

                if (!int.TryParse(
                        fields[0],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var version)
                    || version != MenuFile.CurrentFormatVersion)
                {
                    throw new InvalidDataException(
                        $"CSV row {index + 1} has an unsupported format version.");
                }

                if (!DateTimeOffset.TryParse(
                        fields[1],
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out _))
                {
                    throw new InvalidDataException(
                        $"CSV row {index + 1} has an invalid saved timestamp.");
                }

                var row = JsonSerializer.Deserialize<MenuFile.ItemRow>(
                              fields[2],
                              MenuFile.SerializerOptions)
                          ?? throw new InvalidDataException(
                              $"CSV row {index + 1} has no item data.");

                store.Add(MenuFile.FromRow(row));
            }

            return store;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The CSV menu contains invalid item data.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"The CSV menu contains an invalid item: {ex.Message}", ex);
        }
    }

    private static string CsvField(string value) =>
        "\"" + value.Replace("\"", "\"\"") + "\"";

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var index = 0;

        while (index < line.Length)
        {
            if (line[index] != '"')
                throw new InvalidDataException("CSV fields must be quoted.");

            index++;
            var field = new StringBuilder();
            var closed = false;

            while (index < line.Length)
            {
                if (line[index] != '"')
                {
                    field.Append(line[index++]);
                    continue;
                }

                if (index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index += 2;
                    continue;
                }

                index++;
                closed = true;
                break;
            }

            if (!closed)
                throw new InvalidDataException("A CSV field has an unclosed quote.");

            fields.Add(field.ToString());

            if (index == line.Length)
                break;

            if (line[index] != ',')
                throw new InvalidDataException("Unexpected text after a CSV field.");

            index++;

            if (index == line.Length)
                fields.Add(string.Empty);
        }

        return fields;
    }
}