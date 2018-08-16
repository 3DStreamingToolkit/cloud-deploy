// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloud3DSTKDeployment.Models;
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
        /// Method to return maximum capacity of rendering slots, including pending pools
        /// </summary>
        /// <returns>Returns the max rendering slots capacity</returns>
        int GetMaxRenderingSlotsCapacity();

        /// <summary>
        /// Method to return if the batch client is approaching rendering capacity
        /// </summary>
        /// <param name="totalClients">Total number of connected clients</param>
        /// <param name="renderingServers">A list of rendering servers connected to the signaling server</param>
        /// <param name="deletePoolId">The pool id to be removed, used only for downscaling</param>
        /// <returns>Returns true/false if we are approaching rendering capacity</returns>
        AutoscalingStatus GetAutoscalingStatus(int totalClients, List<ConnectedServer> renderingServers, out string deletePoolId);
        
        /// <summary>
        /// Method to return if the orchestrator is handling auto scaling
        /// </summary>
        /// <returns>Returns if the orchestrator is handling auto scaling</returns>
        bool IsAutoScaling();

        /// <summary>
        /// Method to return if the api has a valid configuration
        /// </summary>
        /// <returns>Returns an empty string for a valid configuration or an error message if not</returns>
        string HasValidConfiguration();

        /// <summary>
        /// Creates a TURN server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be created</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        Task<object> CreateTurnPool(string poolId);

        /// <summary>
        /// Method to wait until pool creation is complete
        /// </summary>
        /// <param name="pool">The pool</param>
        /// <param name="desiredState">The desired state of the pool</param>
        /// <returns>Returns a boolean when the pool is ready</returns>
        Task<bool> AwaitDesiredPoolState(CloudPool pool, AllocationState desiredState);

        /// <summary>
        /// Method to wait until pool creation is complete
        /// </summary>
        /// <param name="node">The compute node</param>
        /// <param name="desiredState">The desired state of the node</param>
        /// <returns>Returns a boolean when the pool is ready</returns>
        Task<bool> AwaitDesiredNodeState(ComputeNode node, ComputeNodeState desiredState);

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
        /// <param name="turnServerIp">The IP of the desired TURN server</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        Task<object> CreateRenderingPool(string poolId, string turnServerIp);

        /// <summary>
        /// Delete a specific pool
        /// </summary>
        /// <param name="poolId">The pool id to be deleted</param>
        /// <returns>Return a task that can be awaited</returns>
        Task DeletePoolAsync(string poolId);
    }
}
