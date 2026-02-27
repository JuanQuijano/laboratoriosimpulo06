using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using rgmanagerweb.Models;

namespace rgmanagerweb.Services;

public class AzureResourceGroupService(ArmClient armClient) : IAzureResourceGroupService
{
    private readonly ArmClient _armClient = armClient;

    public async Task<IReadOnlyList<SubscriptionItem>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = new List<SubscriptionItem>();

        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync(cancellationToken))
        {
            subscriptions.Add(new SubscriptionItem(subscription.Data.SubscriptionId, subscription.Data.DisplayName));
        }

        return subscriptions
            .OrderBy(subscription => subscription.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<ResourceGroupItem>> GetResourceGroupsAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var resourceGroups = new List<ResourceGroupItem>();

        await foreach (var resourceGroup in subscription.GetResourceGroups().GetAllAsync(cancellationToken: cancellationToken))
        {
            resourceGroups.Add(new ResourceGroupItem(resourceGroup.Data.Name, resourceGroup.Data.Location));
        }

        return resourceGroups
            .OrderBy(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task CreateResourceGroupAsync(string subscriptionId, string resourceGroupName, string location, CancellationToken cancellationToken = default)
    {
        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed,
            resourceGroupName,
            new ResourceGroupData(location),
            cancellationToken: cancellationToken);
    }

    public async Task DeleteResourceGroupsAsync(string subscriptionId, IEnumerable<string> resourceGroupNames, CancellationToken cancellationToken = default)
    {
        foreach (var resourceGroupName in resourceGroupNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var resourceGroup = _armClient.GetResourceGroupResource(
                ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName));

            await resourceGroup.DeleteAsync(WaitUntil.Completed, cancellationToken: cancellationToken);
        }
    }
}
