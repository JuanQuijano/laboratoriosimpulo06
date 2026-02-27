using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using rgmanagerweb.Models;
using rgmanagerweb.Services;

namespace rgmanagerweb.Controllers;

[Authorize]
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
        CreateResourceGroupRequest request,
        string? sortColumn,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Debes seleccionar suscripción, nombre y ubicación.";
            return RedirectToAction(nameof(Index), new { subscriptionId = request.SubscriptionId, sortColumn, sortDirection });
        }

        var subscriptionId = request.SubscriptionId.Trim();
        var resourceGroupName = request.ResourceGroupName.Trim();
        var location = request.Location.Trim();

        if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(resourceGroupName) || string.IsNullOrWhiteSpace(location))
        {
            TempData["ErrorMessage"] = "Debes seleccionar suscripción, nombre y ubicación.";
            return RedirectToAction(nameof(Index), new { subscriptionId, sortColumn, sortDirection });
        }

        try
        {
            await _resourceGroupService.CreateResourceGroupAsync(
                subscriptionId,
                resourceGroupName,
                location,
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
        DeleteResourceGroupsRequest request,
        string? sortColumn,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var subscriptionId = request.SubscriptionId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            TempData["ErrorMessage"] = "Debes seleccionar una suscripción.";
            return RedirectToAction(nameof(Index), new { sortColumn, sortDirection });
        }

        var selected = request.SelectedResourceGroups
            .Where(resourceGroupName => !string.IsNullOrWhiteSpace(resourceGroupName))
            .Select(resourceGroupName => resourceGroupName.Trim())
            .ToList();

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
        var normalizedSortColumn = ResourceGroupSorting.NormalizeSortColumn(sortColumn);
        var normalizedSortDirection = ResourceGroupSorting.NormalizeSortDirection(sortDirection);

        var subscriptions = await _resourceGroupService.GetSubscriptionsAsync(cancellationToken);

        var resolvedSubscriptionId = subscriptionId;
        if (string.IsNullOrWhiteSpace(resolvedSubscriptionId))
        {
            resolvedSubscriptionId = subscriptions.FirstOrDefault()?.Id;
        }

        var resourceGroups = new List<ResourceGroupItem>();
        if (!string.IsNullOrWhiteSpace(resolvedSubscriptionId))
        {
            resourceGroups = ResourceGroupSorting
                .Sort(
                    await _resourceGroupService.GetResourceGroupsAsync(resolvedSubscriptionId, cancellationToken),
                    normalizedSortColumn,
                    normalizedSortDirection)
                .ToList();
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
