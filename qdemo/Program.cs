using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;

namespace qdemo
{
    class Program
    {
        static void Main(string[] args)
        {
            string storageConnectionString =
                "DefaultEndpointsProtocol=https;AccountName=dandemostorageaccount;AccountKey=RGbnmbXlvtaL3JFglRpeQyFtnj864acFrVNcMRNHlH9t+J3RWWA2bu9Aafv4/sP7H3+Q1CQ2Rvir/JyqKBLy8g==;EndpointSuffix=core.windows.net";
            string queueName = "purchase-orders";
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);

            Console.WriteLine("Starting queue...");
            queue.CreateIfNotExists();

            Console.WriteLine("sending a message...");
            CloudQueueMessage messageSent = new CloudQueueMessage("Hello, want to play a game?");
            queue.AddMessage(messageSent);

            Console.WriteLine("Reading message");
            CloudQueueMessage messageRead = queue.GetMessage();

            Console.WriteLine($"Message: {messageRead.AsString}");

        }
    }
}
