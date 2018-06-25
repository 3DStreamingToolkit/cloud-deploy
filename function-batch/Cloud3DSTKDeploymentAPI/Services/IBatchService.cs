// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Common;

    /// <summary>
    /// The interface to hold all batch related methods
    /// </summary>
    public interface IBatchService
    {
        /// <summary>
        /// Method to return all pools in a batch client
        /// </summary>
        /// <returns>Returns a list of pools</returns>
        IList<CloudPool> GetPoolsInBatch();

        /// <summary>
        /// Creates a TURN server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be created</param>
        /// <param name="dedicatedNodes">The number of dedicated nodes inside the pool</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        Task<CloudPool> CreateTurnPool(string poolId, int dedicatedNodes);

        /// <summary>
        /// Method to wait until pool creation is complete
        /// </summary>
        /// <param name="pool">The pool</param>
        /// <param name="desiredState">The desired state of the pool</param>
        /// <returns>Returns a boolean when the pool is ready</returns>
        Task<bool> AwaitDesiredPoolState(CloudPool pool, AllocationState desiredState);

        /// <summary>
        /// Resizes a TURN server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be resized</param>
        /// <param name="dedicatedNodes">The new number of dedicated nodes inside the pool</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        Task<bool> ResizeTurnPool(string poolId, int dedicatedNodes);

        /// <summary>
        /// Creates a rendering server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be created</param>
        /// <param name="dedicatedNodes">The number of dedicated nodes inside the pool</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        Task<CloudPool> CreateRenderingPool(string poolId, int dedicatedNodes);

        /// <summary>
        /// Creates the rendering tasks for each node inside the pool
        /// The task runs a PowerShell script to update the signaling and TURN information inside each node
        /// </summary>
        /// <param name="turnServersPool">The TURN server pool id</param>
        /// <param name="jobId">The job id for the tasks</param>
        /// <param name="signalingServerURL">The URI for the signaling server</param>
        /// <param name="signalingServerPort">The port for the signaling server</param>
        /// <param name="serverCapacity">The max number of concurrent users per rendering node</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        Task<bool> AddRenderingTasksAsync(string turnServersPool, string jobId, string signalingServerURL, int signalingServerPort, int serverCapacity);

        /// <summary>
        /// Monitors the specified tasks for completion and returns a value indicating whether all tasks completed successfully
        /// within the timeout period.
        /// </summary>
        /// <param name="jobId">The id of the job containing the tasks that should be monitored.</param>
        /// <param name="timeout">The period of time to wait for the tasks to reach the completed state.</param>
        /// <returns><c>true</c> if all tasks in the specified job completed with an exit code of 0 within the specified timeout period, otherwise <c>false</c>.</returns>
        Task<bool> MonitorTasks(string jobId, TimeSpan timeout);

        /// <summary>
        /// Creates a Job that holds all tasks for that specific pool
        /// </summary>
        /// <param name="jobId">The id for the new job creation</param>
        /// <param name="poolId">The pool id for this job</param>
        /// <returns><c>true</c> if the job was completed</returns>
        Task CreateJobAsync(string jobId, string poolId);

        /// <summary>
        /// Delete a specific job
        /// </summary>
        /// <param name="jobId">The job id to be deleted</param>
        /// <returns>Return a task that can be awaited</returns>
        Task DeleteJobAsync(string jobId);

        /// <summary>
        /// Delete a specific pool
        /// </summary>
        /// <param name="poolId">The pool id to be deleted</param>
        /// <returns>Return a task that can be awaited</returns>
        Task DeletePoolAsync(string poolId);
    }
}
