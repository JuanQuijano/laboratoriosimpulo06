using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using apim.Models;
using apim.Services;

namespace apim.Controllers;

public class HomeController : Controller
{
    private static readonly HashSet<string> AllowedStatuses = ["available", "pending", "sold"];
    private readonly IApimPetstoreService _apimPetstoreService;

    public HomeController(IApimPetstoreService apimPetstoreService)
    {
        _apimPetstoreService = apimPetstoreService;
    }

    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        var selectedStatus = string.IsNullOrWhiteSpace(status)
            ? "available"
            : status.Trim().ToLowerInvariant();

        if (!AllowedStatuses.Contains(selectedStatus))
        {
            selectedStatus = "available";
        }

        var result = await _apimPetstoreService.GetPetsByStatusAsync(selectedStatus, cancellationToken);

        var viewModel = new PetListViewModel
        {
            SelectedStatus = selectedStatus,
            ApiFound = result.ApiFound,
            ApiName = result.ApiName,
            ApiPath = result.ApiPath,
            ErrorMessage = result.ErrorMessage,
            Pets = result.Pets
        };

        return View(viewModel);
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
