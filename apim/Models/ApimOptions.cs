namespace apim.Models;

public class ApimOptions
{
    public const string SectionName = "Apim";

    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ApiDisplayName { get; set; } = "Swagger Petstore";
    public string GatewayBaseUrl { get; set; } = string.Empty;
    public string SubscriptionKey { get; set; } = string.Empty;
}