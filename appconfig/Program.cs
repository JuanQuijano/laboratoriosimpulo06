using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Azure.Identity;

string endpoint = "https://appconfigname16124.azconfig.io"; 

DefaultAzureCredentialOptions credentialOptions = new()
{
    ExcludeEnvironmentCredential = true,
    ExcludeManagedIdentityCredential = true
};

var builder = new ConfigurationBuilder();

builder.AddAzureAppConfiguration(options =>
{
    options.Connect(new Uri(endpoint), new DefaultAzureCredential(credentialOptions));
});

// Build the final configuration object
try
{
    var config = builder.Build();
    Console.WriteLine(config["Dev:conStr"]);
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to Azure App Configuration: {ex.Message}");
}


public class