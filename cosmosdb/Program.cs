using Microsoft.Azure.Cosmos;
using dotenv.net;

var databaseName = "myDatabase";
var containerName = "myContainer";


// Load environment variables from .env file
DotEnv.Load();
var envVars = DotEnv.Read();
var cosmosDbAccountUrl = envVars["DOCUMENT_ENDPOINT"];
var accountKey = envVars["ACCOUNT_KEY"];

if (string.IsNullOrEmpty(cosmosDbAccountUrl) || string.IsNullOrEmpty(accountKey))
{
    Console.WriteLine("Por favor, configura los parámetros DOCUMENT_ENDPOINT y ACCOUNT_KEY en las variables de entorno.");
    return;
}

// CREATE THE COSMOS DB CLIENT USING THE ACCOUNT URL AND KEY
CosmosClient client = new(
    accountEndpoint: cosmosDbAccountUrl,
    authKeyOrResourceToken: accountKey
);


try
{
    // CREATE A DATABASE IF IT DOESN'T ALREADY EXIST
    Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
    Console.WriteLine($"He creado o recuperado la base de datos: {database.Id}");

    // CREATE A CONTAINER WITH A SPECIFIED PARTITION KEY
    Container container = await database.CreateContainerIfNotExistsAsync(
        id: containerName,
        partitionKeyPath: "/id"
    );
    Console.WriteLine($"He creado o recuperado el contenedor: {container.Id}");


    // DEFINE A TYPED ITEM (PRODUCT) TO ADD TO THE CONTAINER


    // ADD THE ITEM TO THE CONTAINER


}
catch (CosmosException ex)
{
    // Handle Cosmos DB-specific exceptions
    // Log the status code and error message for debugging
    Console.WriteLine($"Cosmos DB Error: {ex.StatusCode} - {ex.Message}");
}
catch (Exception ex)
{
    // Handle general exceptions
    // Log the error message for debugging
    Console.WriteLine($"Error: {ex.Message}");
}

// This class represents a product in the Cosmos DB container
public class Product
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
}