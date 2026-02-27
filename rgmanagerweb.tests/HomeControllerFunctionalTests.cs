using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using rgmanagerweb.Controllers;
using rgmanagerweb.Models;
using rgmanagerweb.Services;

namespace rgmanagerweb.tests;

[TestClass]
public sealed class HomeControllerFunctionalTests
{
    [TestMethod]
    public async Task FlujoFuncional_ListaOrdenaAlfabeticamente_CreaYBorraGrupo()
    {
        var service = new FakeResourceGroupService
        {
            Subscriptions = [new SubscriptionItem("sub-001", "Subscripción 1")],
            ResourceGroupsBySubscription =
            {
                ["sub-001"] =
                [
                    new ResourceGroupItem("rg-zeta", "westus"),
                    new ResourceGroupItem("rg-alpha", "eastus")
                ]
            }
        };

        var controller = CreateController(service);

        var listResult = await controller.Index("sub-001", null, null, CancellationToken.None) as ViewResult;
        Assert.IsNotNull(listResult);
        var initialModel = listResult.Model as ResourceGroupManagerViewModel;
        Assert.IsNotNull(initialModel);
        Assert.HasCount(2, initialModel.ResourceGroups);

        var sortedResult = await controller.Index("sub-001", "name", "asc", CancellationToken.None) as ViewResult;
        Assert.IsNotNull(sortedResult);
        var sortedModel = sortedResult.Model as ResourceGroupManagerViewModel;
        Assert.IsNotNull(sortedModel);
        CollectionAssert.AreEqual(
            new[] { "rg-alpha", "rg-zeta" },
            sortedModel.ResourceGroups.Select(resourceGroup => resourceGroup.Name).ToArray());

        var createResult = await controller.CreateResourceGroup(
            request: new CreateResourceGroupRequest
            {
                SubscriptionId = "sub-001",
                ResourceGroupName = "rg-beta",
                Location = "eastus"
            },
            sortColumn: "name",
            sortDirection: "asc",
            cancellationToken: CancellationToken.None);
        Assert.IsInstanceOfType<RedirectToActionResult>(createResult);

        var afterCreateResult = await controller.Index("sub-001", "name", "asc", CancellationToken.None) as ViewResult;
        Assert.IsNotNull(afterCreateResult);
        var afterCreateModel = afterCreateResult.Model as ResourceGroupManagerViewModel;
        Assert.IsNotNull(afterCreateModel);
        CollectionAssert.AreEqual(
            new[] { "rg-alpha", "rg-beta", "rg-zeta" },
            afterCreateModel.ResourceGroups.Select(resourceGroup => resourceGroup.Name).ToArray());

        var deleteResult = await controller.DeleteResourceGroups(
            request: new DeleteResourceGroupsRequest
            {
                SubscriptionId = "sub-001",
                SelectedResourceGroups = ["rg-beta"]
            },
            sortColumn: "name",
            sortDirection: "asc",
            cancellationToken: CancellationToken.None);
        Assert.IsInstanceOfType<RedirectToActionResult>(deleteResult);

        var afterDeleteResult = await controller.Index("sub-001", "name", "asc", CancellationToken.None) as ViewResult;
        Assert.IsNotNull(afterDeleteResult);
        var afterDeleteModel = afterDeleteResult.Model as ResourceGroupManagerViewModel;
        Assert.IsNotNull(afterDeleteModel);
        CollectionAssert.AreEqual(
            new[] { "rg-alpha", "rg-zeta" },
            afterDeleteModel.ResourceGroups.Select(resourceGroup => resourceGroup.Name).ToArray());
    }

    [TestMethod]
    public async Task Index_WithoutSubscription_UsesFirstAndSortsByNameAsc()
    {
        var service = new FakeResourceGroupService
        {
            Subscriptions =
            [
                new SubscriptionItem("sub-001", "Subscripción 1"),
                new SubscriptionItem("sub-002", "Subscripción 2")
            ],
            ResourceGroupsBySubscription =
            {
                ["sub-001"] =
                [
                    new ResourceGroupItem("rg-zeta", "westus"),
                    new ResourceGroupItem("rg-alpha", "eastus")
                ]
            }
        };

        var controller = CreateController(service);

        var result = await controller.Index(subscriptionId: null, sortColumn: null, sortDirection: null, CancellationToken.None);

        var viewResult = result as ViewResult;
        Assert.IsNotNull(viewResult);

        var model = viewResult.Model as ResourceGroupManagerViewModel;
        Assert.IsNotNull(model);

        Assert.AreEqual("sub-001", model.SelectedSubscriptionId);
        Assert.AreEqual("name", model.SortColumn);
        Assert.AreEqual("asc", model.SortDirection);
        CollectionAssert.AreEqual(new[] { "rg-alpha", "rg-zeta" }, model.ResourceGroups.Select(resourceGroup => resourceGroup.Name).ToArray());
    }

    [TestMethod]
    public async Task Index_WithSortByLocationDesc_SortsByLocationThenName()
    {
        var service = new FakeResourceGroupService
        {
            Subscriptions = [new SubscriptionItem("sub-001", "Subscripción 1")],
            ResourceGroupsBySubscription =
            {
                ["sub-001"] =
                [
                    new ResourceGroupItem("rg-a", "eastus"),
                    new ResourceGroupItem("rg-c", "westus"),
                    new ResourceGroupItem("rg-b", "westus")
                ]
            }
        };

        var controller = CreateController(service);

        var result = await controller.Index("sub-001", "location", "desc", CancellationToken.None);

        var viewResult = result as ViewResult;
        Assert.IsNotNull(viewResult);

        var model = viewResult.Model as ResourceGroupManagerViewModel;
        Assert.IsNotNull(model);

        CollectionAssert.AreEqual(
            new[] { "rg-b", "rg-c", "rg-a" },
            model.ResourceGroups.Select(resourceGroup => resourceGroup.Name).ToArray());
    }

    [TestMethod]
    public async Task CreateResourceGroup_WithInvalidInput_SetsErrorAndRedirects()
    {
        var service = new FakeResourceGroupService();
        var controller = CreateController(service);

        var result = await controller.CreateResourceGroup(
            request: new CreateResourceGroupRequest
            {
                SubscriptionId = "sub-001",
                ResourceGroupName = string.Empty,
                Location = "eastus"
            },
            sortColumn: "name",
            sortDirection: "asc",
            cancellationToken: CancellationToken.None);

        var redirectResult = result as RedirectToActionResult;
        Assert.IsNotNull(redirectResult);
        Assert.AreEqual("Index", redirectResult.ActionName);
        Assert.AreEqual("sub-001", redirectResult.RouteValues?["subscriptionId"]);
        Assert.AreEqual("name", redirectResult.RouteValues?["sortColumn"]);
        Assert.AreEqual("asc", redirectResult.RouteValues?["sortDirection"]);
        Assert.AreEqual("Debes seleccionar suscripción, nombre y ubicación.", controller.TempData["ErrorMessage"]);
        Assert.IsNull(service.LastCreateRequest);
    }

    [TestMethod]
    public async Task CreateResourceGroup_WithValidInput_CallsServiceAndSetsSuccess()
    {
        var service = new FakeResourceGroupService();
        var controller = CreateController(service);

        var result = await controller.CreateResourceGroup(
            request: new CreateResourceGroupRequest
            {
                SubscriptionId = "sub-001",
                ResourceGroupName = "  rg-demo  ",
                Location = " eastus "
            },
            sortColumn: "location",
            sortDirection: "desc",
            cancellationToken: CancellationToken.None);

        var redirectResult = result as RedirectToActionResult;
        Assert.IsNotNull(redirectResult);

        Assert.IsNotNull(service.LastCreateRequest);
        Assert.AreEqual("sub-001", service.LastCreateRequest.Value.SubscriptionId);
        Assert.AreEqual("rg-demo", service.LastCreateRequest.Value.ResourceGroupName);
        Assert.AreEqual("eastus", service.LastCreateRequest.Value.Location);
        StringAssert.Contains(controller.TempData["SuccessMessage"]?.ToString(), "creado correctamente");
    }

    [TestMethod]
    public async Task DeleteResourceGroups_WithoutSelection_SetsErrorAndRedirects()
    {
        var service = new FakeResourceGroupService();
        var controller = CreateController(service);

        var result = await controller.DeleteResourceGroups(
            request: new DeleteResourceGroupsRequest
            {
                SubscriptionId = "sub-001",
                SelectedResourceGroups = []
            },
            sortColumn: "name",
            sortDirection: "asc",
            cancellationToken: CancellationToken.None);

        var redirectResult = result as RedirectToActionResult;
        Assert.IsNotNull(redirectResult);
        Assert.AreEqual("Index", redirectResult.ActionName);
        Assert.AreEqual("Selecciona al menos un grupo de recursos para borrar.", controller.TempData["ErrorMessage"]);
        Assert.IsNull(service.LastDeleteRequest);
    }

    [TestMethod]
    public async Task DeleteResourceGroups_WithSelection_CallsServiceAndSetsSuccessCount()
    {
        var service = new FakeResourceGroupService();
        var controller = CreateController(service);

        var result = await controller.DeleteResourceGroups(
            request: new DeleteResourceGroupsRequest
            {
                SubscriptionId = "sub-001",
                SelectedResourceGroups = ["rg-a", "rg-b"]
            },
            sortColumn: "location",
            sortDirection: "desc",
            cancellationToken: CancellationToken.None);

        var redirectResult = result as RedirectToActionResult;
        Assert.IsNotNull(redirectResult);

        Assert.IsNotNull(service.LastDeleteRequest);
        Assert.AreEqual("sub-001", service.LastDeleteRequest.Value.SubscriptionId);
        CollectionAssert.AreEqual(new[] { "rg-a", "rg-b" }, service.LastDeleteRequest.Value.ResourceGroupNames.ToArray());
        Assert.AreEqual("Se eliminaron 2 grupo(s) de recursos correctamente.", controller.TempData["SuccessMessage"]);
    }

    private static HomeController CreateController(FakeResourceGroupService service)
    {
        return new HomeController(service)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new InMemoryTempDataProvider())
        };
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object?> _tempData = [];

        public IDictionary<string, object?> LoadTempData(HttpContext context)
        {
            return _tempData;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
        {
            _tempData = new Dictionary<string, object?>(values);
        }
    }

    private sealed class FakeResourceGroupService : IAzureResourceGroupService
    {
        public List<SubscriptionItem> Subscriptions { get; set; } = [];

        public Dictionary<string, List<ResourceGroupItem>> ResourceGroupsBySubscription { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public (string SubscriptionId, string ResourceGroupName, string Location)? LastCreateRequest { get; private set; }

        public (string SubscriptionId, IReadOnlyList<string> ResourceGroupNames)? LastDeleteRequest { get; private set; }

        public Task<IReadOnlyList<SubscriptionItem>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SubscriptionItem>>(Subscriptions);
        }

        public Task<IReadOnlyList<ResourceGroupItem>> GetResourceGroupsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        {
            ResourceGroupsBySubscription.TryGetValue(subscriptionId, out var resourceGroups);
            return Task.FromResult<IReadOnlyList<ResourceGroupItem>>(resourceGroups ?? []);
        }

        public Task CreateResourceGroupAsync(string subscriptionId, string resourceGroupName, string location, CancellationToken cancellationToken = default)
        {
            LastCreateRequest = (subscriptionId, resourceGroupName, location);

            if (!ResourceGroupsBySubscription.TryGetValue(subscriptionId, out var resourceGroups))
            {
                resourceGroups = [];
                ResourceGroupsBySubscription[subscriptionId] = resourceGroups;
            }

            if (!resourceGroups.Any(resourceGroup => string.Equals(resourceGroup.Name, resourceGroupName, StringComparison.OrdinalIgnoreCase)))
            {
                resourceGroups.Add(new ResourceGroupItem(resourceGroupName, location));
            }

            return Task.CompletedTask;
        }

        public Task DeleteResourceGroupsAsync(string subscriptionId, IEnumerable<string> resourceGroupNames, CancellationToken cancellationToken = default)
        {
            var names = resourceGroupNames.ToList();
            LastDeleteRequest = (subscriptionId, names);

            if (ResourceGroupsBySubscription.TryGetValue(subscriptionId, out var resourceGroups))
            {
                resourceGroups.RemoveAll(resourceGroup => names.Any(name => string.Equals(name, resourceGroup.Name, StringComparison.OrdinalIgnoreCase)));
            }

            return Task.CompletedTask;
        }
    }
}
