using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using rgmanagerweb.Models;
using rgmanagerweb.Services;

namespace rgmanagerweb.Controllers;

public class HomeController(IAzureResourceGroupService resourceGroupService) : Controller
{
    private readonly IAzureResourceGroupService _resourceGroupService = resourceGroupService;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? subscriptionId,
        string? sortColumn,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(subscriptionId, sortColumn, sortDirection, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResourceGroup(
        string subscriptionId,
        string resourceGroupName,
        string location,
        string? sortColumn,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(resourceGroupName) || string.IsNullOrWhiteSpace(location))
        {
            TempData["ErrorMessage"] = "Debes seleccionar suscripción, nombre y ubicación.";
            return RedirectToAction(nameof(Index), new { subscriptionId, sortColumn, sortDirection });
        }

        try
        {
            await _resourceGroupService.CreateResourceGroupAsync(
                subscriptionId,
                resourceGroupName.Trim(),
                location.Trim(),
                cancellationToken);

            TempData["SuccessMessage"] = $"Grupo de recursos '{resourceGroupName}' creado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"No fue posible crear el grupo de recursos: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { subscriptionId, sortColumn, sortDirection });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResourceGroups(
        string subscriptionId,
        List<string>? selectedResourceGroups,
        string? sortColumn,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            TempData["ErrorMessage"] = "Debes seleccionar una suscripción.";
            return RedirectToAction(nameof(Index), new { sortColumn, sortDirection });
        }

        var selected = selectedResourceGroups ?? [];

        if (selected.Count == 0)
        {
            TempData["ErrorMessage"] = "Selecciona al menos un grupo de recursos para borrar.";
            return RedirectToAction(nameof(Index), new { subscriptionId, sortColumn, sortDirection });
        }

        try
        {
            await _resourceGroupService.DeleteResourceGroupsAsync(subscriptionId, selected, cancellationToken);
            TempData["SuccessMessage"] = $"Se eliminaron {selected.Count} grupo(s) de recursos correctamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"No fue posible eliminar los grupos de recursos: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { subscriptionId, sortColumn, sortDirection });
    }

    private async Task<ResourceGroupManagerViewModel> BuildViewModelAsync(
        string? subscriptionId,
        string? sortColumn,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var normalizedSortColumn = sortColumn?.Trim().ToLowerInvariant() switch
        {
            "location" => "location",
            _ => "name"
        };

        var normalizedSortDirection = sortDirection?.Trim().ToLowerInvariant() == "desc" ? "desc" : "asc";

        var subscriptions = await _resourceGroupService.GetSubscriptionsAsync(cancellationToken);

        var resolvedSubscriptionId = subscriptionId;
        if (string.IsNullOrWhiteSpace(resolvedSubscriptionId))
        {
            resolvedSubscriptionId = subscriptions.FirstOrDefault()?.Id;
        }

        var resourceGroups = new List<ResourceGroupItem>();
        if (!string.IsNullOrWhiteSpace(resolvedSubscriptionId))
        {
            resourceGroups = (await _resourceGroupService.GetResourceGroupsAsync(resolvedSubscriptionId, cancellationToken)).ToList();

            resourceGroups = normalizedSortColumn switch
            {
                "location" when normalizedSortDirection == "desc" => resourceGroups
                    .OrderByDescending(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                "location" => resourceGroups
                    .OrderBy(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                _ when normalizedSortDirection == "desc" => resourceGroups
                    .OrderByDescending(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                _ => resourceGroups
                    .OrderBy(resourceGroup => resourceGroup.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(resourceGroup => resourceGroup.Location, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        return new ResourceGroupManagerViewModel
        {
            SelectedSubscriptionId = resolvedSubscriptionId,
            Subscriptions = subscriptions,
            ResourceGroups = resourceGroups,
            SortColumn = normalizedSortColumn,
            SortDirection = normalizedSortDirection
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
