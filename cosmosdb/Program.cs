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
    double totalRequestCharge = 0;

    // CREATE A DATABASE IF IT DOESN'T ALREADY EXIST
    (Database database, double databaseRequestCharge) = await CreateDatabaseIfNotExistsWithChargeAsync(client, databaseName);
    totalRequestCharge += databaseRequestCharge;
    Console.WriteLine($"Costo de conectar con la base de datos: {databaseRequestCharge} RUs");

    // CREATE A CONTAINER WITH A SPECIFIED PARTITION KEY
    (Container container, double containerRequestCharge) = await CreateContainerIfNotExistsWithChargeAsync(
        database,
        containerName,
        "/id"
    );
    totalRequestCharge += containerRequestCharge;
    Console.WriteLine($"Costo de conectar con el contenedor: {containerRequestCharge} RUs");


    // DEFINE A TYPED ITEM (PRODUCT) TO ADD TO THE CONTAINER
    Product newItem = new Product
    {
        id = Guid.NewGuid().ToString(), // Generate a unique ID for the product
        name = "Sample Item",
        description = "Este es un item de ejemplo para el laboratorio de Cosmos DB en el curso AZ-204."
    };


    // ADD THE ITEM TO THE CONTAINER
    (Product createdProduct, double createItemRequestCharge) = await CreateProductWithChargeAsync(container, newItem);
    totalRequestCharge += createItemRequestCharge;

    Console.WriteLine($"Costo de creación del item: {createItemRequestCharge} RUs");

    // QUERY THE CONTAINER TO RETRIEVE THE ITEM WE JUST CREATED
    (Product? recoveredProduct, double queryRequestCharge) = await QueryProductByIdWithChargeAsync(container, newItem.id);
    totalRequestCharge += queryRequestCharge;
    Console.WriteLine($"Costo de recuperar un item: {queryRequestCharge} RUs");

    if (recoveredProduct is not null)
    {
        (Product updatedProduct, double updateRequestCharge) = await UpdateProductDescriptionAsync(
            container,
            recoveredProduct,
            "Descripción actualizada después de recuperar el item"
        );
        totalRequestCharge += updateRequestCharge;
        Console.WriteLine($"Costo de la actualización de un item: {updateRequestCharge} RUs");

        if (!string.IsNullOrEmpty(updatedProduct.id))
        {
            double deleteRequestCharge = await DeleteProductWithChargeAsync(container, updatedProduct.id);
            totalRequestCharge += deleteRequestCharge;
            Console.WriteLine($"Costo de eliminación de un item: {deleteRequestCharge} RUs");
        }
    }

    Console.WriteLine($"Costo total de la ejecución de las operaciones CRUD: {totalRequestCharge:F2} RUs");
    

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

async Task<(Product UpdatedProduct, double RequestCharge)> UpdateProductDescriptionAsync(Container container, Product product, string newDescription)
{
    product.description = newDescription;
    ItemResponse<Product> updateResponse = await container.ReplaceItemAsync(
        item: product,
        id: product.id,
        partitionKey: new PartitionKey(product.id)
    );

    return (updateResponse.Resource, updateResponse.RequestCharge);
}

async Task<double> DeleteProductWithChargeAsync(Container container, string productId)
{
    ItemResponse<Product> deleteResponse = await container.DeleteItemAsync<Product>(
        id: productId,
        partitionKey: new PartitionKey(productId)
    );

    return deleteResponse.RequestCharge;
}

async Task<(Database Database, double RequestCharge)> CreateDatabaseIfNotExistsWithChargeAsync(CosmosClient client, string dbName)
{
    DatabaseResponse databaseResponse = await client.CreateDatabaseIfNotExistsAsync(dbName);
    return (databaseResponse.Database, databaseResponse.RequestCharge);
}

async Task<(Container Container, double RequestCharge)> CreateContainerIfNotExistsWithChargeAsync(Database database, string contName, string partitionKeyPath)
{
    ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync(
        id: contName,
        partitionKeyPath: partitionKeyPath
    );
    return (containerResponse.Container, containerResponse.RequestCharge);
}

async Task<(Product CreatedProduct, double RequestCharge)> CreateProductWithChargeAsync(Container container, Product product)
{
    ItemResponse<Product> createResponse = await container.CreateItemAsync(
        item: product,
        partitionKey: new PartitionKey(product.id)
    );
    return (createResponse.Resource, createResponse.RequestCharge);
}

async Task<(Product? Product, double RequestCharge)> QueryProductByIdWithChargeAsync(Container container, string productId)
{
    QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
        .WithParameter("@id", productId);

    FeedIterator<Product> queryResultSetIterator = container.GetItemQueryIterator<Product>(queryDefinition);
    double totalQueryRequestCharge = 0;
    Product? recoveredProduct = null;

    while (queryResultSetIterator.HasMoreResults)
    {
        FeedResponse<Product> currentResultSet = await queryResultSetIterator.ReadNextAsync();
        totalQueryRequestCharge += currentResultSet.RequestCharge;

        foreach (Product product in currentResultSet)
        {
            recoveredProduct = product;
        }
    }

    return (recoveredProduct, totalQueryRequestCharge);
}

// This class represents a product in the Cosmos DB container
public class Product
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
}