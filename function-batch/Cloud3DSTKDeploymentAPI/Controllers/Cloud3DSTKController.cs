// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloud3DSTKDeploymentAPI.Models;
    using Cloud3DSTKDeploymentAPI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Api controller for 3D Streaming Toolkit cloud scaling 
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class Cloud3DSTKController : Controller
    {
        /// <summary>
        /// Interface to the batch service
        /// </summary>
        private readonly IBatchService batchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cloud3DSTKApiController" /> class
        /// </summary>
        /// <param name="batchService">An instance of the batch service</param>
        public Cloud3DSTKController(IBatchService batchService)
        {
            this.batchService = batchService;
        }

        /// <summary>
        /// The create api 
        /// </summary>
        /// <param name="jsonBody">The post json body</param>
        /// <returns>The call result as a <see cref="IActionResult" /> class</returns>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Post(
            [FromBody] JObject jsonBody)
        {
            if (jsonBody == null)
            {
                return this.BadRequest(ApiResultMessages.ErrorBodyIsEmpty);
            }

            var signalingServer = jsonBody["signalingServer"]?.ToObject<string>();
            var signalingServerPort = jsonBody["signalingServerPort"]?.ToObject<int>();
            string turnPoolId = jsonBody["turnPoolId"]?.ToObject<string>() ?? "DefaultTurnPool";
            string renderingPoolId = jsonBody["renderingPoolId"]?.ToObject<string>() ?? "DefaultRenderingPool";
            int dedicatedTurnNodes = jsonBody["dedicatedTurnNodes"]?.ToObject<int>() ?? 1;
            int dedicatedRenderingNodes = jsonBody["dedicatedRenderingNodes"]?.ToObject<int>() ?? 1;
            int maxUsersPerRenderingNode = jsonBody["maxUsersPerRenderingNode"]?.ToObject<int>() ?? -1;
            string renderingJobId = jsonBody["renderingJobId"]?.ToObject<string>() ?? "3DSTKRenderingJob";

            // Signaling URL and port is required
            if (string.IsNullOrEmpty(signalingServer) || !signalingServerPort.HasValue)
            {
                return this.BadRequest(ApiResultMessages.ErrorNoSignalingFound);
            }

            // Each pool must have at least one dedicated node
            if (dedicatedRenderingNodes < 1 || dedicatedTurnNodes < 1)
            {
                return this.BadRequest(ApiResultMessages.ErrorOneDedicatedNodeRequired);
            }

            // Each rendering node must have at least one max user
            if (maxUsersPerRenderingNode < 1)
            {
                return this.BadRequest(ApiResultMessages.ErrorOneMaxUserRequired);
            }

            var turnPool = this.batchService.GetPoolsInBatch().FirstOrDefault((s) => s.Id == turnPoolId);
            if (turnPool == null)
            {
                await this.batchService.CreateTurnPool(turnPoolId, dedicatedTurnNodes);
            }

            var renderingPool = this.batchService.GetPoolsInBatch().FirstOrDefault((s) => s.Id == renderingPoolId);
            if (renderingPool == null)
            {
                await this.batchService.CreateRenderingPool(renderingPoolId, dedicatedRenderingNodes);

                await this.batchService.CreateJobAsync(renderingJobId, renderingPoolId);
                for (var i = 0; i < dedicatedRenderingNodes; i++)
                {
                    await this.batchService.AddRenderingTasksAsync(turnPoolId, renderingJobId, signalingServer, signalingServerPort.Value, maxUsersPerRenderingNode);
                }

                await this.batchService.MonitorTasks(renderingJobId, new TimeSpan(0, 20, 0));
            }
            
            return this.Ok();
        }

        /// <summary>
        /// The delete batch pool api 
        /// </summary>
        /// <param name="jsonBody">The post json body</param>
        /// <returns>The call result as a <see cref="IActionResult" /> class</returns>
        [HttpPost]
        [Route("deleteBatchPool")]
        public async Task<IActionResult> DeletePool(
            [FromBody] JObject jsonBody)
        {
            if (jsonBody == null)
            {
                return this.BadRequest(ApiResultMessages.ErrorBodyIsEmpty);
            }

            var poolId = jsonBody["poolId"]?.ToObject<string>();

            // Pool Id is required
            if (string.IsNullOrEmpty(poolId))
            {
                return this.BadRequest(ApiResultMessages.ErrorPoolIdRequired);
            }

            var pool = this.batchService.GetPoolsInBatch().FirstOrDefault(p => p.Id == poolId);
            if (pool == null)
            {
                return this.BadRequest(ApiResultMessages.ErrorPoolIdNotFound);
            }
            else
            {
                await this.batchService.DeletePoolAsync(poolId);
            }
            
            return this.Ok();
        }
    }
}
