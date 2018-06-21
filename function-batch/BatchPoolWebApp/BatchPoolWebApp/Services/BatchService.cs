using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatchPoolWebApp.Models;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Extensions.Configuration;

namespace BatchPoolWebApp.Services
{
    public class BatchService : IBatchService
    {
        public static IConfiguration Configuration { get; set; }

        // Batch account credentials
        private static string BatchAccountName;
        private static string BatchAccountKey;
        private static string BatchAccountUrl;

        public BatchService(IConfiguration configuration)
        {
            Configuration = configuration;

            BatchAccountName = Configuration["BatchAccountName"];
            BatchAccountKey = Configuration["BatchAccountKey"];
            BatchAccountUrl = Configuration["BatchAccountUrl"];
        }

        public Task<PoolModel[]> GetPoolDataAsync()
        {
            var poolItem1 = new PoolModel
            {
                PoolId = "1",
                AllocationState = AllocationState.Resizing
            };

            var poolItem2 = new PoolModel
            {
                PoolId = "2",
                AllocationState = AllocationState.Steady
            };
            var poolItem3 = new PoolModel
            {
                PoolId = "3",
                AllocationState = AllocationState.Stopping
            };

            return Task.FromResult(new[] { poolItem1, poolItem2, poolItem3 });
        }

        public IList<CloudPool> GetPoolsInBatch()
        {
            BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

            using (BatchClient batchClient = BatchClient.Open(cred))
            {
                var poolData = batchClient.PoolOperations.ListPools();
                return poolData.ToList();
            }
        }
    }
}
