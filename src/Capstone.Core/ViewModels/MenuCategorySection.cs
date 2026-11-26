using System.Collections.ObjectModel;
using Capstone.Core.Models;

namespace Capstone.Core.ViewModels;

public sealed class MenuCategorySection(string name)
{
    public string Name { get; } = name;
    public ObservableCollection<MenuItem> Items { get; } = [];
}
