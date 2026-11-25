namespace Capstone.Core.Models;

public sealed class CategoryRegistry
{
    private readonly Dictionary<string, Category> _categories =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<Category> Categories =>
        _categories.Values;

    public void Add(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (_categories.ContainsKey(category.Name))
        {
            throw new ArgumentException(
                $"A category named '{category.Name}' already exists.",
                nameof(category));
        }

        _categories.Add(category.Name, category);
    }
}