using System.Linq;
using SharpQuest.Contracts;

namespace Capstone.Core;

public static class ItemQueries
{
    public static IEnumerable<IItem> Longest(this IEnumerable<IItem> items, int n)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return items
            .OrderByDescending(item => item.DisplayName.Length)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(n);
    }
}