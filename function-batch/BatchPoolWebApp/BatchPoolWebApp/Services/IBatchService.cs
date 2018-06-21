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
        Task<bool> CreateWindowsPool(string poolId, int dedicatedNodes);
        Task<List<CloudTask>> AddTasksAsync(BatchClient batchClient, string turnServersPool, string jobId, string signalingServerURL, int signalingServerPort, bool isWindows);
        Task<bool> MonitorTasks(BatchClient batchClient, string jobId, TimeSpan timeout);
    }
}
