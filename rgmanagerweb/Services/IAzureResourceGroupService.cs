using rgmanagerweb.Models;

namespace rgmanagerweb.Services;

public interface IAzureResourceGroupService
{
    Task<IReadOnlyList<SubscriptionItem>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceGroupItem>> GetResourceGroupsAsync(string subscriptionId, CancellationToken cancellationToken = default);

    Task CreateResourceGroupAsync(string subscriptionId, string resourceGroupName, string location, CancellationToken cancellationToken = default);

    Task DeleteResourceGroupsAsync(string subscriptionId, IEnumerable<string> resourceGroupNames, CancellationToken cancellationToken = default);
}
