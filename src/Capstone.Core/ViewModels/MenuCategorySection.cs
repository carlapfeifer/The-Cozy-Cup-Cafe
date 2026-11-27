using System.Collections.ObjectModel;
using Capstone.Core.Models;

namespace Capstone.Core.ViewModels;

public sealed class MenuCategorySection(string name, bool isSoldOutSection = false)
{
    public string Name { get; } = name;
    public bool IsSoldOutSection { get; } = isSoldOutSection;
    public ObservableCollection<MenuItem> Items { get; } = [];
}
