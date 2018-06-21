using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatchPoolWebApp.Models;

namespace BatchPoolWebApp.Services
{
    public class BatchService : IBatchService
    {
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
    }
}
