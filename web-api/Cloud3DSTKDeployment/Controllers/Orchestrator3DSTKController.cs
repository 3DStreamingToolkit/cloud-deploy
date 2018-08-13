// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Web.Http;
    using System.Web.Http.Hosting;
    using Cloud3DSTKDeployment.Helpers;
    using Cloud3DSTKDeployment.Models;
    using Cloud3DSTKDeployment.Services;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Common;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Api controller for 3D Streaming Toolkit cloud orchestration
    /// </summary>
    public class Orchestrator3DSTKController : ApiController
    {
        /// <summary>
        /// Interface to the batch service
        /// </summary>
        private IBatchService batchService;

        /// <summary>
        /// Timer to downscale after a period of time
        /// </summary>
        private Timer downscaleTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Orchestrator3DSTKController" /> class
        /// </summary>
        public Orchestrator3DSTKController()
        {
            this.InitBatchService(ConfigurationHelper.GetConfiguration());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Orchestrator3DSTKController" /> class
        /// </summary>
        /// <param name="configuration">The configuration used for the batch service</param> with a custom configuration
        public Orchestrator3DSTKController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            this.InitBatchService(configuration);
        }

        /// <summary>
        /// The create api 
        /// </summary>
        /// <param name="jsonBody">The post json body</param>
        /// <returns>The call result as a <see cref="IActionResult" /> class</returns>
        [HttpPost]
        [Route("api/orchestrator")]
        public async Task<HttpResponseMessage> Post(
            [FromBody] JObject jsonBody)
        {
            if (jsonBody == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorBodyIsEmpty);
            }

            var totalClients = jsonBody["totalSessions"]?.ToObject<int>();
            var totalSlots = jsonBody["totalSlots"]?.ToObject<int>();
            var serversList = jsonBody["servers"]?.ToObject<JObject>();
            var connectedServers = new List<ConnectedServer>();

            if (serversList.HasValues)
            {
                foreach (var server in serversList)
                {
                    connectedServers.Add(server.Value.ToObject<ConnectedServer>());
                }
            }

            if (!totalClients.HasValue || !totalSlots.HasValue)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            var deletePoolId = string.Empty;
            switch (this.batchService.GetAutoscalingStatus(totalClients.Value, connectedServers, out deletePoolId))
            {
                case AutoscalingStatus.NotEnabled:
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.WarningNoAutoscaling);
                    }

                case AutoscalingStatus.UpscaleRenderingPool:
                    {
                        if (this.downscaleTimer != null && this.downscaleTimer.Enabled)
                        {
                            // We are approaching capacity, stop any down scale timer
                            this.downscaleTimer.Stop();
                        }

                        var controller = new Cloud3DSTKController
                        {
                            Request = new HttpRequestMessage()
                        };

                        controller.Request.Properties.Add(
                            HttpPropertyKeys.HttpConfigurationKey,
                            new HttpConfiguration());

                        // Create a json body with a specific pool and job ID. These should be unique to avoid conflict. 
                        var json = new CreateApiJsonBody
                        {
                            RenderingPoolId = Guid.NewGuid().ToString()
                        };

                        // Spin up a new Rendering Pool
                        var result = await controller.Post((JObject)JToken.FromObject(json));

                        break;
                    }

                case AutoscalingStatus.DownscaleRenderingPool:
                    {
                        if (this.downscaleTimer != null && !this.downscaleTimer.Enabled)
                        {
                            this.downscaleTimer.Elapsed += (sender, e) => this.DownscaleElapsedMethod(sender, e, totalClients.Value, connectedServers);

                            // Wait until the downscale threshold timout is met and delete the pool
                            this.downscaleTimer.Start();
                        }

                        break;
                    }

                case AutoscalingStatus.OK:
                    {
                        if (this.downscaleTimer != null && this.downscaleTimer.Enabled)
                        {
                            // We are in normal parameters, stop any down scale timer
                            this.downscaleTimer.Stop();
                        }

                        break;
                    }
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// Helper method to initialize the Batch Service and downscale timer
        /// </summary>
        /// <param name="configuration">The configuration used for the batch service</param> with a custom configuration
        private void InitBatchService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            this.batchService = new BatchService(configuration);

            int downscaleTimeoutMinutes = -1;
            int.TryParse(configuration["AutomaticDownscaleTimeoutMinutes"], out downscaleTimeoutMinutes);
            
            if (downscaleTimeoutMinutes > 0)
            {
                this.downscaleTimer = new Timer(new TimeSpan(0, downscaleTimeoutMinutes, 0).TotalMilliseconds);
            }
        }

        /// <summary>
        /// Downscale timer elapsed method
        /// </summary>
        /// <param name="sender">The timer object</param>
        /// <param name="e">Elapsed event arguments</param>
        /// <param name="totalClients">Total number of clients connected to the signaling server</param>
        /// <param name="servers">The list of servers </param>
        private async void DownscaleElapsedMethod(object sender, ElapsedEventArgs e, int totalClients, List<ConnectedServer> servers)
        {
            var deletePoolId = string.Empty;
            if (this.batchService.GetAutoscalingStatus(totalClients, servers, out deletePoolId) == AutoscalingStatus.DownscaleRenderingPool)
            {
                var controller = new Cloud3DSTKController
                {
                    Request = new HttpRequestMessage()
                };

                controller.Request.Properties.Add(
                    HttpPropertyKeys.HttpConfigurationKey,
                    new HttpConfiguration());

                // Create a json body with the pool id to be deleted
                var json = new DeletePoolApiJsonBody
                {
                    PoolId = deletePoolId
                };

                // Scale down and remove pool
                var result = await controller.DeletePool((JObject)JToken.FromObject(json));
            }

            this.downscaleTimer.Stop();
        }
    }
}
