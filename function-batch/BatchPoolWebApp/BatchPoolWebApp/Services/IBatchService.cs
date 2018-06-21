using BatchPoolWebApp.Models;
using Microsoft.Azure.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BatchPoolWebApp.Services
{
    public interface IBatchService
    {
        Task<PoolModel[]> GetPoolDataAsync();
        IList<CloudPool> GetPoolsInBatch();
    }
}
