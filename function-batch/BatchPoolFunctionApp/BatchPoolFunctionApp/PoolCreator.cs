
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;

namespace BatchPoolFunctionApp
{
    public static class PoolCreator
    {
        // Azure AD + integrated auth credentials 
        private static string AuthorityUri = GetEnvironmentVariable("AuthorityUri");
        private static string BatchResourceUri = GetEnvironmentVariable("BatchResourceUri");
        private static string ClientId = GetEnvironmentVariable("ClientId");
        private static string RedirectUri = GetEnvironmentVariable("RedirectUri");

        // Batch account credentials
        private static string BatchAccountName = GetEnvironmentVariable("BatchAccountName");
        private static string BatchAccountKey = GetEnvironmentVariable("BatchAccountKey");
        private static string BatchAccountUrl = GetEnvironmentVariable("BatchAccountUrl");

        // Storage account credentials
        private static string StorageAccountName = GetEnvironmentVariable("StorageAccountName");
        private static string StorageAccountKey = GetEnvironmentVariable("StorageAccountKey");

        // Custom Pool/Job settings
        private static string PoolId = GetEnvironmentVariable("PoolId");
        private static string JobId = GetEnvironmentVariable("JobId");

        // VM image settings
        private static string VirtualMachineImageId = GetEnvironmentVariable("VirtualMachineImageId");


        [FunctionName("PoolCreator")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // batch tasks
            CreateBatchPool(req, log);

            // placeholder tasks

            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static void CreateBatchPool(HttpRequest req, TraceWriter log)
        {
            log.Info($"CreateBatchPool triggered, {JobId}");
        }

    }
}
