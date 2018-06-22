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
        Task<bool> CreateLinuxPool(string poolId, int dedicatedNodes);
        Task<bool> CreateWindowsPool(string turnServersPool, string poolId, int dedicatedNodes, string signalingServerURL, int signalingServerPort);
        Task<bool> MonitorTasks(string jobId, TimeSpan timeout);
        Task CreateJobAsync(string jobId, string poolId);
        Task DeleteJobAsync(string jobId);
        Task DeletePoolAsync(string poolId);
    }
}
