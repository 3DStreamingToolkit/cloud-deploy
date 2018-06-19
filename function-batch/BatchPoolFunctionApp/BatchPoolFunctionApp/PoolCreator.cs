
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using System.Reflection;

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
            InitBatchPool(req, log).Wait();

            // placeholder tasks
            return new OkObjectResult("success");

            //string name = req.Query["name"];

            //string requestBody = new StreamReader(req.Body).ReadToEnd();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //return name != null
            //    ? (ActionResult)new OkObjectResult($"Hello, {name}")
            //    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static async Task InitBatchPool(HttpRequest req, TraceWriter log)
        {
            if (String.IsNullOrEmpty(BatchAccountName) || String.IsNullOrEmpty(BatchAccountKey) || String.IsNullOrEmpty(BatchAccountUrl) ||
                String.IsNullOrEmpty(StorageAccountName) || String.IsNullOrEmpty(StorageAccountKey))
            {
                throw new InvalidOperationException("One ore more account credential strings have not been populated. Please ensure that your Batch and Storage account credentials have been specified.");
            }

            try
            {
                await CreateBatchPool(req, log);
            }
            catch (ReflectionTypeLoadException typeLoadException)
            {
                var loaderExceptions = typeLoadException.LoaderExceptions;

                log.Info("Loader Exceptions occurred...");
                foreach (var le in loaderExceptions)
                    log.Info(le.Message);
            }
            catch (AggregateException ae)
            {
                log.Info("One or more exceptions occurred.");
                PrintAggregateException(ae, log);
            }
            finally
            {
                log.Info("Init complete.");
            }
        }

        public static void PrintAggregateException(AggregateException aggregateException, TraceWriter log)
        {
            // Flatten the aggregate and iterate over its inner exceptions, printing each
            foreach (Exception exception in aggregateException.Flatten().InnerExceptions)
            {
                log.Info(exception.ToString());
            }
        }

        private static async Task CreateBatchPool(HttpRequest req, TraceWriter log)
        {
            log.Info($"CreateBatchPool triggered, {PoolId}");

            // get auth token
            //Func<Task<string>> tokenProvider = () => GetAuthenticationTokenAsync();
            Func<Task<string>> tokenProvider = () => null;

            //using (var batchClient = BatchClient.Open(new BatchTokenCredentials(BatchAccountUrl, tokenProvider)))
            BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

            using (BatchClient batchClient = BatchClient.Open(cred))
            {
                await CreatePoolIfNotExistAsync(batchClient, PoolId, log);
            }
        }

        private static async Task CreatePoolIfNotExistAsync(BatchClient batchClient, string poolId, TraceWriter log)
        {
            CloudPool pool = null;
            try
            {
                log.Info($"Creating pool [{0}]... {poolId}");

                //pool = batchClient.PoolOperations.CreatePool(
                //    poolId: poolId,
                //    targetDedicatedComputeNodes: 1,
                //    virtualMachineSize: "Standard_NV6",
                //    virtualMachineConfiguration: new VirtualMachineConfiguration(
                //        new ImageReference(virtualMachineImageId: VirtualMachineImageId),
                //        nodeAgentSkuId: "batch.node.windows amd64")
                //    );

                pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: 1,
                    virtualMachineSize: "Standard_NV6",
                    virtualMachineConfiguration: new VirtualMachineConfiguration(
                        new ImageReference(
                            offer: "WindowsServer",
                            publisher: "MicrosoftWindowsServer",
                            sku: "2016-DataCenter",
                            version: "latest"),
                        nodeAgentSkuId: "batch.node.windows amd64")
                    );

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                // Swallow the specific error code PoolExists since that is expected if the pool already exists
                if (be.RequestInformation?.BatchError != null && be.RequestInformation.BatchError.Code == BatchErrorCodeStrings.PoolExists)
                {
                    log.Info("The pool {0} already existed when we tried to create it", poolId);
                }
            }
            catch (Exception ex)
            {
                log.Info($"Unexpected error occurred: {ex.Message}");
            }
        }

        //public static async Task<string> GetAuthenticationTokenAsync()
        //{
        //    var authContext = new AuthenticationContext(AuthorityUri);

        //    // Acquire the authentication token from Azure AD.
        //    var authResult = await authContext.AcquireTokenAsync(BatchResourceUri,
        //                                                        ClientId,
        //                                                        new Uri(RedirectUri),
        //                                                        new PlatformParameters());

        //    return authResult.AccessToken;
        //}

    }
}
