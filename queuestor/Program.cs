using Azure;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Threading.Tasks;

string queueName = "myqueue-trainerdemosjqa";
string storageAccountName = "storactname13258";


DefaultAzureCredentialOptions options = new()
{
    ExcludeEnvironmentCredential = true,
    ExcludeManagedIdentityCredential = true
};

// ADD CODE TO CREATE A QUEUE CLIENT AND CREATE A QUEUE
QueueClient queueClient = new QueueClient(
    new Uri($"https://{storageAccountName}.queue.core.windows.net/{queueName}"),
    new DefaultAzureCredential(options));

Console.WriteLine($"Creating queue: {queueName}");
await queueClient.CreateIfNotExistsAsync();
Console.WriteLine("Queue created, press Enter to add messages to the queue...");
Console.ReadLine();


// ADD CODE TO SEND AND LIST MESSAGES
await queueClient.SendMessageAsync("Message 1");
await queueClient.SendMessageAsync("Message 2");
await queueClient.SendMessageAsync("Message 3");
await queueClient.SendMessageAsync("Message 4");

SendReceipt receipt = await queueClient.SendMessageAsync("Message 5");

Console.WriteLine("Messages added to the queue. Press Enter to peek at the messages...");
Console.ReadLine();

foreach (var message in (await queueClient.PeekMessagesAsync(maxMessages: 32)).Value)
{
    Console.WriteLine($"Message: {message.MessageText}");
}

Console.WriteLine("\nPress Enter to update a message in the queue...");
Console.ReadLine();

// ADD CODE TO UPDATE A MESSAGE AND LIST MESSAGES
await queueClient.UpdateMessageAsync(receipt.MessageId, receipt.PopReceipt, "Message 5 has been updated");

Console.WriteLine("Message five updated. Press Enter to peek at the messages again...");
Console.ReadLine();


foreach (var message in (await queueClient.PeekMessagesAsync(maxMessages: 32)).Value)
{
    Console.WriteLine($"Message: {message.MessageText}");
}

Console.WriteLine("\nPress Enter to delete messages from the queue...");
Console.ReadLine();


// ADD CODE TO DELETE MESSAGES AND THE QUEUE
foreach (var message in (await queueClient.ReceiveMessagesAsync(maxMessages: 32)).Value)
{
    // "Process" the message
    Console.WriteLine($"Deleting message: {message.MessageText}");

    // Let the service know we're finished with the message and it can be safely deleted.
    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
}

Console.WriteLine("Messages deleted from the queue.");
Console.WriteLine("\nPress Enter key to delete the queue...");
Console.ReadLine();

Console.WriteLine($"Deleting queue: {queueClient.Name}");
await queueClient.DeleteAsync();

Console.WriteLine("Done");
