// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Controllers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Cloud3DSTKDeployment.Helpers;
    using Cloud3DSTKDeployment.Models;
    using Cloud3DSTKDeployment.Services;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Common;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Api controller for 3D Streaming Toolkit cloud scaling 
    /// </summary>
    public class Cloud3DSTKController : ApiController
    {
        /// <summary>
        /// Interface to the batch service
        /// </summary>
        private readonly IBatchService batchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cloud3DSTKController" /> class
        /// </summary>
        public Cloud3DSTKController()
        {
            this.batchService = new BatchService(ConfigurationHelper.GetConfiguration());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cloud3DSTKController" /> class with a custom configuration
        /// </summary>
        /// <param name="configuration">The configuration used for the batch service</param>
        public Cloud3DSTKController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            this.batchService = new BatchService(configuration);
        }

        /// <summary>
        /// The create api 
        /// </summary>
        /// <param name="jsonBody">The post json body</param>
        /// <returns>The call result as a <see cref="IActionResult" /> class</returns>
        [HttpPost]
        [Route("api/create")]
        public async Task<HttpResponseMessage> Post(
            [FromBody] JObject jsonBody)
        {
            string turnPoolId = "DefaultTurnPool";
            string renderingPoolId = "DefaultRenderingPool";
            string renderingJobId = "3DSTKRenderingJob";

            if (jsonBody != null)
            {
                turnPoolId = jsonBody["turnPoolId"]?.ToObject<string>() ?? "DefaultTurnPool";
                renderingPoolId = jsonBody["renderingPoolId"]?.ToObject<string>() ?? "DefaultRenderingPool";
                renderingJobId = jsonBody["renderingJobId"]?.ToObject<string>() ?? "3DSTKRenderingJob";
            }
            
            var configurationCheckMessage = this.batchService.HasValidConfiguration();

            // Check for a valid configuration
            if (!string.IsNullOrWhiteSpace(configurationCheckMessage))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, configurationCheckMessage);
            }
          
            // Check if rendering pool already exists
            var renderingPool = this.batchService.GetPoolsInBatch().FirstOrDefault((s) => s.Id == renderingPoolId);
            if (renderingPool != null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorRenderingPoolExists);
            }

            // Create the rendering job
            var createJobResult = await this.batchService.CreateJobAsync(renderingJobId, renderingPoolId);
            if (!string.IsNullOrEmpty(createJobResult))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorJobExists);
            }

            // Create the TURN pool 
            var turnPool = this.batchService.GetPoolsInBatch().FirstOrDefault((s) => s.Id == turnPoolId);
            if (turnPool == null)
            {
                var turnPoolObject = await this.batchService.CreateTurnPool(turnPoolId);
                if (turnPoolObject.GetType() == typeof(string))
                {
                    await this.batchService.DeleteJobAsync(renderingJobId);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, turnPoolObject);
                }

                turnPool = (CloudPool)turnPoolObject;
                
                var turnCreationResult = await this.batchService.AwaitDesiredPoolState(turnPool, AllocationState.Steady);
                if (!turnCreationResult)
                {
                    await this.batchService.DeleteJobAsync(renderingJobId);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorToCreateTurnPool);
                }
            }

            // Retrieve top TURN server node
            var turnNodes = turnPool.ListComputeNodes();
            var topNode = turnNodes.FirstOrDefault();

            if (topNode == null)
            {
                await this.batchService.DeleteJobAsync(renderingJobId);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ApiResultMessages.ErrorTurnServerInvalid);
            }

            // Create the rendering pool 
            var renderingPoolObject = await this.batchService.CreateRenderingPool(renderingPoolId);
            if (renderingPoolObject.GetType() == typeof(string))
            {
                await this.batchService.DeleteJobAsync(renderingJobId);
                return Request.CreateResponse(HttpStatusCode.BadRequest, renderingPoolObject);
            }

            renderingPool = (CloudPool)renderingPoolObject;

            var renderingCreationResult = await this.batchService.AwaitDesiredPoolState(renderingPool, AllocationState.Steady);
            if (!renderingCreationResult)
            {
                await this.batchService.DeleteJobAsync(renderingJobId);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ApiResultMessages.ErrorToCreateRenderingPool);
            }

            // Wait until the TURN node is ready
            var result = await this.batchService.AwaitDesiredNodeState(topNode, ComputeNodeState.Idle);
            if (!result)
            {
                await this.batchService.DeleteJobAsync(renderingJobId);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ApiResultMessages.ErrorTurnServerInvalid);
            }
            
            var rdp = topNode.GetRemoteLoginSettings();

            // Wait until all rendering nodes are ready
            var renderingNodes = renderingPool.ListComputeNodes();
            int readyNodes = renderingNodes.Count();

            foreach (var node in renderingNodes)
            {
                var nodeResult = await this.batchService.AwaitDesiredNodeState(node, ComputeNodeState.Idle);
                if (!nodeResult)
                {
                    try
                    {
                        // This call may throw an exception even when completed successfully
                        await node.RemoveFromPoolAsync();
                    }
                    catch
                    {
                    }

                    readyNodes--;
                }
            }
            
            // Each task will add the correct TURN and signaling uri for the rendering server 
            // Azure batch runs tasks as part of a queue so any node can take them
            // Since we waited for all nodes to be ready, we can now spread the tasks evenly 
            for (var i = 0; i < readyNodes; i++)
            {
                await this.batchService.AddRenderingTasksAsync(rdp.IPAddress, renderingJobId);
            }

            await this.batchService.MonitorTasks(renderingJobId, new TimeSpan(0, 20, 0));
            await this.batchService.DeleteJobAsync(renderingJobId);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// The delete batch pool api 
        /// </summary>
        /// <param name="jsonBody">The post json body</param>
        /// <returns>The call result as a <see cref="IActionResult" /> class</returns>
        [HttpPost]
        [Route("api/deletePool")]
        public async Task<HttpResponseMessage> DeletePool(
            [FromBody] JObject jsonBody)
        {
            if (jsonBody == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorBodyIsEmpty);
            }

            var poolId = jsonBody["poolId"]?.ToObject<string>();

            // Pool Id is required
            if (string.IsNullOrEmpty(poolId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorPoolIdRequired);
            }

            var pool = this.batchService.GetPoolsInBatch().FirstOrDefault(p => p.Id == poolId);
            if (pool == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorPoolIdNotFound);
            }
            else
            {
                await this.batchService.DeletePoolAsync(poolId);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
