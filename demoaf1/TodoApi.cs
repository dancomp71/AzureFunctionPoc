using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace demoaf1
{
    public static class TodoApi
    {
        public const string StorageConnectionStr = "AzureWebJobsStorage";
        public const string StorageTableName = "todos";
        public const string RouteUrl = "todo";
        public const string IdUrl = "{id}";
        public const string RouteIdUrl = RouteUrl + "/" + IdUrl;
        public const string PartitionKey = "TODO";

        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = RouteUrl)] HttpRequest req,
            [Table(StorageTableName, Connection = StorageConnectionStr)] IAsyncCollector<TodoTableEntity> todoTable,
            ILogger log)
        {
            log.LogInformation("Creating a new todo list item");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            TodoCreateModel todoItem = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo()
            {
                TaskDescription = todoItem.TaskDescription
            };

            await todoTable.AddAsync(todo.To());

            return new OkObjectResult(todo);

            //return name != null
            //    ? (ActionResult)new OkObjectResult($"Hello, {name}")
            //    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("GetTodo")]
        public static async Task<IActionResult> GetTodo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = RouteUrl)] HttpRequest req,
            [Table(StorageTableName, Connection = StorageConnectionStr)] CloudTable todoTable,
            ILogger log)
        {
            log.LogInformation("Getting todo list from CloudTable");
            var query = new TableQuery<TodoTableEntity>();
            var segment = await todoTable.ExecuteQuerySegmentedAsync(query, null);
            return new ObjectResult(segment.Select(Mappings.To));
        }

        [FunctionName("GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = RouteIdUrl)] HttpRequest req,
            [Table(StorageTableName, PartitionKey, "{id}", Connection = StorageConnectionStr)] TodoTableEntity todo,
            ILogger log,
            string id)
        {
            log.LogInformation("Getting todo item by id");
            if (todo == null)
            {
                log.LogInformation($"todo [{id}] not found.");
                return new NotFoundResult();
            }
            return new ObjectResult(todo.To());
        }

        [FunctionName("UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = RouteIdUrl)] HttpRequest req,
            [Table(StorageTableName, Connection = StorageConnectionStr)] CloudTable todoTable,
            ILogger log,
            string id)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
            var findOp = TableOperation.Retrieve<TodoTableEntity>(PartitionKey, id);
            var findResult = await todoTable.ExecuteAsync(findOp);
            if (findResult.Result == null)
            {
                return new NotFoundResult();
            }

            var todo = (TodoTableEntity)findResult.Result;

            todo.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.TaskDescription))
            {
                todo.TaskDescription = updated.TaskDescription;
            }

            var replaceOp = TableOperation.Replace(todo);
            await todoTable.ExecuteAsync(replaceOp);
            return new OkObjectResult(todo.To());
        }

        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = RouteIdUrl)] HttpRequest req,
            [Table(StorageTableName, Connection = StorageConnectionStr)] CloudTable todoTable,
            ILogger log,
            string id)
        {
            //var todo = items.FirstOrDefault(t => t.Id == id);
            //if (todo == null)
            //{
            //    log.LogInformation($"todo [{id}] not found.");
            //    return new NotFoundResult();
            //}
            //items.Remove(todo);
            //log.LogInformation($"todo [{id}] removed.");
            //return new OkObjectResult(todo);

            var delOp = TableOperation.Delete(new TodoTableEntity()
            {
                PartitionKey = PartitionKey,
                RowKey = id,
                ETag = "*"
            });
            try
            {
                var deleteResult = await todoTable.ExecuteAsync(delOp);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }
            return new OkResult();
        }
    }
}
