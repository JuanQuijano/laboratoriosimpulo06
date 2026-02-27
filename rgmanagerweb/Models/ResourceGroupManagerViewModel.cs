namespace rgmanagerweb.Models;

public class ResourceGroupManagerViewModel
{
    public string? SelectedSubscriptionId { get; set; }

    public string SortColumn { get; set; } = "name";

    public string SortDirection { get; set; } = "asc";

    public string NextNameSortDirection => SortColumn == "name" && SortDirection == "asc" ? "desc" : "asc";

    public string NextLocationSortDirection => SortColumn == "location" && SortDirection == "asc" ? "desc" : "asc";

    public IReadOnlyList<SubscriptionItem> Subscriptions { get; set; } = [];

    public IReadOnlyList<ResourceGroupItem> ResourceGroups { get; set; } = [];
}

public record SubscriptionItem(string Id, string DisplayName);

public record ResourceGroupItem(string Name, string Location);
