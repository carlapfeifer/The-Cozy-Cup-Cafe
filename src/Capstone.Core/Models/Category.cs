namespace Capstone.Core.Models;

public record Category(string Id, string Name)
{
    public string Id { get; init; } = !string.IsNullOrWhiteSpace(Id)
        ? Id
        : throw new ArgumentException("Id is required.", nameof(Id));

    public string Name { get; init; } = !string.IsNullOrWhiteSpace(Name)
        ? Name
        : throw new ArgumentException("Name is required.", nameof(Name));
}