using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace demoaf1
{
    public static class QueueSubscriber
    {
        [FunctionName("QueueSubscribers")]
        public static async Task Run(
            [QueueTrigger(
                TodoApi.QueueName, 
                Connection = TodoApi.StorageConnectionStr)]Todo todo, 
            [Blob(
                TodoApi.BlobBinding, 
                Connection = TodoApi.StorageConnectionStr)] CloudBlobContainer container,
            ILogger log)
        {
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference($"{todo.Id}.txt");
            await blob.UploadTextAsync($"Created a new task: {todo.TaskDescription}");
            log.LogInformation($"C# Queue trigger function processed: {todo.TaskDescription}");
        }
    }
}
