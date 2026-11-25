using Capstone.Core.Models;

namespace Capstone.Core.Subscribers;

public sealed class AvailabilityTransitionLog
{
    private readonly List<MenuItem> _restockedItems = new();

    public IReadOnlyList<MenuItem> RestockedItems => _restockedItems.ToArray();

    public MenuSubscription Attach(CafeStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        EventHandler<AvailabilityChangedChange> handler = (_, change) =>
        {
            if (!change.PreviousAvailability && change.IsAvailable
                                             && change.Item is MenuItem menuItem)
            {
                _restockedItems.Add(menuItem);
            }
        };

        store.AvailabilityChanged += handler;
        return new MenuSubscription(() => store.AvailabilityChanged -= handler);
    }
}