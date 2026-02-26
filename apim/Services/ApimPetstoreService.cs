using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using apim.Models;

namespace apim.Services;

public interface IApimPetstoreService
{
    Task<PetSearchResult> GetPetsByStatusAsync(string status, CancellationToken cancellationToken);
}

public class ApimPetstoreService : IApimPetstoreService
{
    private static readonly HashSet<string> AllowedStatuses = ["available", "pending", "sold"];
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly ApimOptions _options;
    private readonly TokenCredential _credential;

    public ApimPetstoreService(HttpClient httpClient, IOptions<ApimOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _credential = new DefaultAzureCredential();
    }

    public async Task<PetSearchResult> GetPetsByStatusAsync(string status, CancellationToken cancellationToken)
    {
        var normalizedStatus = status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
        {
            return new PetSearchResult
            {
                ErrorMessage = "Estado no válido. Usa available, pending o sold."
            };
        }

        if (string.IsNullOrWhiteSpace(_options.SubscriptionId)
            || string.IsNullOrWhiteSpace(_options.ResourceGroupName)
            || string.IsNullOrWhiteSpace(_options.ServiceName))
        {
            return new PetSearchResult
            {
                ErrorMessage = "Configura Apim:SubscriptionId, Apim:ResourceGroupName y Apim:ServiceName."
            };
        }

        try
        {
            var api = await FindApiByDisplayNameAsync(cancellationToken);
            if (api is null)
            {
                return new PetSearchResult
                {
                    ApiFound = false,
                    ApiName = _options.ApiDisplayName,
                    ErrorMessage = $"No se encontró la API '{_options.ApiDisplayName}' en APIM."
                };
            }

            var apiPath = api.Path.Trim('/');
            var gatewayBaseUrl = ResolveGatewayBaseUrl().TrimEnd('/');
            var requestUri = $"{gatewayBaseUrl}/{apiPath}/pet/findByStatus?status={Uri.EscapeDataString(normalizedStatus)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            if (!string.IsNullOrWhiteSpace(_options.SubscriptionKey))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new PetSearchResult
                {
                    ApiFound = true,
                    ApiName = api.DisplayName,
                    ApiPath = apiPath,
                    ErrorMessage = $"Error consultando mascotas en APIM: {(int)response.StatusCode} {response.ReasonPhrase}. {body}"
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var pets = await JsonSerializer.DeserializeAsync<List<PetResponse>>(stream, JsonOptions, cancellationToken) ?? [];

            return new PetSearchResult
            {
                ApiFound = true,
                ApiName = api.DisplayName,
                ApiPath = apiPath,
                Pets = pets.Select(p => new PetItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Status = p.Status,
                    CategoryName = p.Category?.Name ?? string.Empty
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new PetSearchResult
            {
                ErrorMessage = $"No fue posible consultar APIM: {ex.Message}"
            };
        }
    }

    private async Task<ApimApiItem?> FindApiByDisplayNameAsync(CancellationToken cancellationToken)
    {
        var managementUri = $"https://management.azure.com/subscriptions/{_options.SubscriptionId}/resourceGroups/{_options.ResourceGroupName}/providers/Microsoft.ApiManagement/service/{_options.ServiceName}/apis?api-version=2022-08-01";
        using var request = new HttpRequestMessage(HttpMethod.Get, managementUri);

        var accessToken = await _credential.GetTokenAsync(
            new TokenRequestContext(["https://management.azure.com/.default"]),
            cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apisResponse = await JsonSerializer.DeserializeAsync<ApimApisResponse>(stream, JsonOptions, cancellationToken);

        return apisResponse?.Value?
            .Select(item => new ApimApiItem
            {
                DisplayName = item.Properties?.DisplayName ?? string.Empty,
                Path = item.Properties?.Path ?? string.Empty
            })
            .FirstOrDefault(api => string.Equals(api.DisplayName, _options.ApiDisplayName, StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveGatewayBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.GatewayBaseUrl))
        {
            return _options.GatewayBaseUrl;
        }

        return $"https://{_options.ServiceName}.azure-api.net";
    }

    private class ApimApisResponse
    {
        public List<ApimApiValue>? Value { get; set; }
    }

    private class ApimApiValue
    {
        public ApimApiProperties? Properties { get; set; }
    }

    private class ApimApiProperties
    {
        public string? DisplayName { get; set; }
        public string? Path { get; set; }
    }

    private class ApimApiItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    private class PetResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PetCategory? Category { get; set; }
    }

    private class PetCategory
    {
        public string Name { get; set; } = string.Empty;
    }
}