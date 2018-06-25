using Microsoft.Azure.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud3DSTKDeploymentAPI.Services
{
    public interface IBatchService
    {
        IList<CloudPool> GetPoolsInBatch();
        Task<bool> CreateLinuxPool(string poolId, int dedicatedNodes);
        Task<bool> CreateWindowsPool(string poolId, int dedicatedNodes);
        Task<bool> AddWindowsTasksAsync(string turnServersPool, string jobId, string signalingServerURL, int signalingServerPort, int serverCapacity);
        Task<bool> MonitorTasks(string jobId, TimeSpan timeout);
        Task CreateJobAsync(string jobId, string poolId);
        Task DeleteJobAsync(string jobId);
        Task DeletePoolAsync(string poolId);
    }
}
