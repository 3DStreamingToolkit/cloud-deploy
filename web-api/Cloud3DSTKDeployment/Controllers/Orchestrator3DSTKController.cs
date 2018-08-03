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
    /// Api controller for 3D Streaming Toolkit cloud orchestration
    /// </summary>
    public class Orchestrator3DSTKController : ApiController
    {
        /// <summary>
        /// Interface to the batch service
        /// </summary>
        private readonly IBatchService batchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cloud3DSTKController" /> class
        /// </summary>
        public Orchestrator3DSTKController()
        {
            this.batchService = new BatchService(ConfigurationHelper.GetConfiguration());
        }

        /// <summary>
        /// The create api 
        /// </summary>
        /// <param name="jsonBody">The post json body</param>
        /// <returns>The call result as a <see cref="IActionResult" /> class</returns>
        [HttpPost]
        [Route("api/signaling")]
        public async Task<HttpResponseMessage> Post(
            [FromBody] JObject jsonBody)
        {
            if (jsonBody == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResultMessages.ErrorBodyIsEmpty);
            }

            var totalClients = jsonBody["totalClients"]?.ToObject<int>();
            var totalSlots = jsonBody["totalSlots"]?.ToObject<int>();

            if (!totalClients.HasValue || !totalSlots.HasValue)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
