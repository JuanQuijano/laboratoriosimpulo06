using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Timers;

string svcbusNameSpace = "svcbusns30838.servicebus.windows.net";
string queueName = "myqueue";

DefaultAzureCredentialOptions options = new()
{
    ExcludeEnvironmentCredential = true,
    ExcludeManagedIdentityCredential = true
};


// ADD CODE TO CREATE A SERVICE BUS CLIENT
ServiceBusClient client = new(svcbusNameSpace, new DefaultAzureCredential(options));


// ADD CODE TO SEND MESSAGES TO THE QUEUE
// Create a sender for the specified queue
ServiceBusSender sender = client.CreateSender(queueName);

using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

const int numOfMessages = 5;

for (int i = 1; i <= numOfMessages; i++)
{
    // try adding a message to the batch
    messageBatch.TryAddMessage(new ServiceBusMessage($"Message {i}"));
}

await sender.SendMessagesAsync(messageBatch);
Console.WriteLine($"A batch of {numOfMessages} messages has been published to the queue.");

await sender.DisposeAsync();

Console.WriteLine("Press any key to continue");
Console.ReadKey();


// ADD CODE TO PROCESS MESSAGES FROM THE QUEUE
ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

const int idleTimeoutMs = 3000;
System.Timers.Timer idleTimer = new(idleTimeoutMs);
idleTimer.Elapsed += async (s, e) =>
{
    Console.WriteLine($"No messages received for {idleTimeoutMs / 1000} seconds. Stopping processor...");
    await processor.StopProcessingAsync();
};

processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

idleTimer.Start();
await processor.StartProcessingAsync();
Console.WriteLine($"Processor started. Will stop after {idleTimeoutMs / 1000} seconds of inactivity.");

while (processor.IsProcessing)
{
    await Task.Delay(500);
}

idleTimer.Stop();
Console.WriteLine("Stopped receiving messages");

await processor.DisposeAsync();
await client.DisposeAsync();


 async Task MessageHandler(ProcessMessageEventArgs args)
{
    var body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");

    // Reset the idle timer on each message
    idleTimer.Stop();
    idleTimer.Start();

    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}