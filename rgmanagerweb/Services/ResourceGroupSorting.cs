using rgmanagerweb.Models;

namespace rgmanagerweb.Services;

public static class ResourceGroupSorting
{
    public static string NormalizeSortColumn(string? sortColumn)
    {
        return sortColumn?.Trim().ToLowerInvariant() switch
        {
            "location" => "location",
            _ => "name"
        };
    }

    public static string NormalizeSortDirection(string? sortDirection)
    {
        return sortDirection?.Trim().ToLowerInvariant() == "desc" ? "desc" : "asc";
    }

    public static IReadOnlyList<ResourceGroupItem> Sort(
        IEnumerable<ResourceGroupItem> resourceGroups,
        string sortColumn,
        string sortDirection)
    {
        var groups = resourceGroups.ToList();

        return sortColumn switch
        {
            "location" when sortDirection == "desc" => groups
                .OrderByDescending(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                .ThenBy(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "location" => groups
                .OrderBy(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                .ThenBy(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ when sortDirection == "desc" => groups
                .OrderByDescending(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => groups
                .OrderBy(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }
}