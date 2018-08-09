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
        private readonly IBatchService batchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Orchestrator3DSTKController" /> class
        /// </summary>
        public Orchestrator3DSTKController()
        {
            this.batchService = new BatchService(ConfigurationHelper.GetConfiguration());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Orchestrator3DSTKController" /> class
        /// </summary>
        /// <param name="configuration">The configuration used for the batch service</param> with a custom configuration
        public Orchestrator3DSTKController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            this.batchService = new BatchService(ConfigurationHelper.GetConfiguration());
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

            if (!this.batchService.IsAutoScaling())
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.WarningNoAutoscaling);
            }

            var totalClients = jsonBody["totalSessions"]?.ToObject<int>();
            var totalSlots = jsonBody["totalSlots"]?.ToObject<int>();

            if (!totalClients.HasValue || !totalSlots.HasValue)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            
            if (this.batchService.ApproachingRenderingCapacity(totalClients.Value))
            {
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
            }
            
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
