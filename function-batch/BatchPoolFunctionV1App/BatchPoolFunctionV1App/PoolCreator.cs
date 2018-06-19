using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace BatchPoolFunctionV1App
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
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // batch tasks
            await InitBatchPool(req, log); //.Wait();

            // placeholder tasks
            return req.CreateResponse(HttpStatusCode.OK, "Success!");

            //// parse query parameter
            //string name = req.GetQueryNameValuePairs()
            //    .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
            //    .Value;

            //if (name == null)
            //{
            //    // Get request body
            //    dynamic data = await req.Content.ReadAsAsync<object>();
            //    name = data?.name;
            //}

            //return name == null
            //    ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            //    : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
        }


        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        private static async Task InitBatchPool(HttpRequestMessage req, TraceWriter log)
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

        private static async Task CreateBatchPool(HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"CreateBatchPool triggered, {PoolId}");

            Func<Task<string>> tokenProvider = () => GetAuthenticationTokenAsync();
            //BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

            //using (BatchClient batchClient = BatchClient.Open(cred))

            using (var batchClient = await BatchClient.OpenAsync(new BatchTokenCredentials(BatchAccountUrl, tokenProvider)))
            {
                await CreatePoolIfNotExistAsync(batchClient, PoolId, log);
            }
        }
        
        public static async Task<string> GetAuthenticationTokenAsync()
        {
            var authContext = new AuthenticationContext(AuthorityUri);

            // Acquire the authentication token from Azure AD.
            var authResult = await authContext.AcquireTokenAsync(BatchResourceUri,
                                                                ClientId,
                                                                new Uri(RedirectUri),
                                                                new PlatformParameters(PromptBehavior.Auto));

            return authResult.AccessToken;
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
                //        new ImageReference(
                //            offer: "WindowsServer",
                //            publisher: "MicrosoftWindowsServer",
                //            sku: "2016-DataCenter",
                //            version: "latest"),
                //        nodeAgentSkuId: "batch.node.windows amd64")
                //    );


                pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: 1,
                    virtualMachineSize: "Standard_NV6",
                    virtualMachineConfiguration: new VirtualMachineConfiguration(
                        new ImageReference(virtualMachineImageId: VirtualMachineImageId),
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
    }
}
