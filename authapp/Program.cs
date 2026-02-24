using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using dotenv.net;

// Load environment variables from .env file
DotEnv.Load();
var envVars = DotEnv.Read();

// Retrieve Azure AD Application ID and tenant ID from environment variables
string _clientId = envVars["CLIENT_ID"];
string _tenantId = envVars["TENANT_ID"];

// Define the scopes required for authentication
string[] _scopes = { "User.Read" };

// Build the MSAL public client application with authority and redirect URI
var app = PublicClientApplicationBuilder.Create(_clientId)
    .WithAuthority(AzureCloudInstance.AzurePublic, _tenantId)
    .WithDefaultRedirectUri()
    .Build();

// Configure token caching
var storageProperties = new StorageCreationPropertiesBuilder("token_cache.bin", Directory.GetCurrentDirectory())
    .WithLinuxKeyring(
        "com.microsoft.identity.client.caller",
        "default",
        "MSALCache",
        new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.Identity.Client"),
        new KeyValuePair<string, string>("MsalClientVersion", "1.0.0.0"))
    .Build();
var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
cacheHelper.RegisterCache(app.UserTokenCache);

// ADD CODE TO ACQUIRE AN ACCESS TOKEN
// Attempt to acquire an access token silently or interactively
AuthenticationResult result;
try
{
    // Try to acquire token silently from cache for the first available account
    var accounts = await app.GetAccountsAsync();
    result = await app.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
}
catch (MsalUiRequiredException)
{
    // If silent token acquisition fails, use device code flow
    result = await app.AcquireTokenWithDeviceCode(_scopes, deviceCodeResult =>
    {
        Console.WriteLine(deviceCodeResult.Message);
        return Task.FromResult(0);
    }).ExecuteAsync();
}

// Output the acquired access token to the console
Console.WriteLine($"Access Token:\n{result.AccessToken}");
