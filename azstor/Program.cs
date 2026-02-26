using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DotNetEnv;

Console.WriteLine("Azure Blob Storage exercise\n");

// Load environment variables from .env file
DotNetEnv.Env.Load();

// Get credentials from environment variables
string? accountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
string? sasToken = Environment.GetEnvironmentVariable("STORAGE_SAS_TOKEN");

if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(sasToken))
{
    Console.WriteLine("Error: STORAGE_ACCOUNT_NAME and STORAGE_SAS_TOKEN must be set in .env file");
    return;
}

// Run the examples asynchronously, wait for the results before proceeding
await ProcessAsync();

Console.WriteLine("\nPress enter to exit the sample application.");
Console.ReadLine();

async Task ProcessAsync()
{
    // CREATE A BLOB STORAGE CLIENT
    // Create the BlobServiceClient using the SAS token
    string blobServiceEndpoint = $"https://{accountName}.blob.core.windows.net";
    BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri($"{blobServiceEndpoint}?{sasToken}"));


    // CREATE A CONTAINER
    // Create a unique name for the container
    string containerName = "wtblob" + Guid.NewGuid().ToString();

    // Create the container and return a container client object
    Console.WriteLine("Creating container: " + containerName);
    BlobContainerClient containerClient =
        await blobServiceClient.CreateBlobContainerAsync(containerName);


    // CREATE A LOCAL FILE FOR UPLOAD TO BLOB STORAGE
// Create a local file in the ./data/ directory for uploading and downloading
Console.WriteLine("Creating a local file for upload to Blob storage...");
string localPath = "./data/";
string fileName = "wtfile" + Guid.NewGuid().ToString() + ".txt";
string localFilePath = Path.Combine(localPath, fileName);

// Write text to the file
await File.WriteAllTextAsync(localFilePath, "Hello, World!");
Console.WriteLine("Local file created, press 'Enter' to continue.");
Console.ReadLine();


    // UPLOAD THE FILE TO BLOB STORAGE
    // Get a reference to the blob and upload the file
BlobClient blobClient = containerClient.GetBlobClient(fileName);

Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}", blobClient.Uri);

// Open the file and upload its data
using (FileStream uploadFileStream = File.OpenRead(localFilePath))
{
    await blobClient.UploadAsync(uploadFileStream);
    uploadFileStream.Close();
}


    // DOWNLOAD THE BLOB TO A LOCAL FILE
// Adds the string "DOWNLOADED" before the .txt extension so it doesn't 
// overwrite the original file

string downloadFilePath = localFilePath.Replace(".txt", "DOWNLOADED.txt");

Console.WriteLine("Downloading blob to: {0}", downloadFilePath);

// Download the blob's contents and save it to a file
BlobDownloadInfo download = await blobClient.DownloadAsync();

using (FileStream downloadFileStream = File.OpenWrite(downloadFilePath))
{
    await download.Content.CopyToAsync(downloadFileStream);
}

Console.WriteLine("Blob downloaded successfully to: {0}", downloadFilePath);

// READ AND DISPLAY THE DOWNLOADED FILE CONTENT
Console.WriteLine("\n--- Contenido del fichero descargado ---");
string fileContent = await File.ReadAllTextAsync(downloadFilePath);
Console.WriteLine(fileContent);
Console.WriteLine("--- Fin del contenido ---\n");

}
