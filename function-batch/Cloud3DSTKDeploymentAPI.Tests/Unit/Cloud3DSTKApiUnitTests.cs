// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Tests
{
    using System.Threading.Tasks;
    using Cloud3DSTKDeploymentAPI.Controllers;
    using Cloud3DSTKDeploymentAPI.Models;
    using Cloud3DSTKDeploymentAPI.Services;
    using Cloud3DSTKDeploymentAPI.Tests.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The unit tests for the 3DSTK cloud scaling api
    /// </summary>
    [TestClass]
    public class Cloud3DSTKApiUnitTests
    {
        /// <summary>
        /// Interface to the appsettings configuration file
        /// </summary>
        private IConfiguration configuration;

        /// <summary>
        /// Unit test for no body post
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        public async Task CreateApiReturnsInvalidRequestWhenNoBody()
        {  
            // Read the appsettings json
            this.SetupConfiguration();

            var batchService = new BatchService(this.configuration);
            var controller = new Cloud3DSTKApiController(batchService);

            var result = await controller.Post(null);
            
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.IsTrue((result as ObjectResult).Value.ToString().Equals(ApiResultMessages.ErrorBodyIsEmpty));
        }

        /// <summary>
        /// Unit test for no signaling uri or port in the post body
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        public async Task CreateApiReturnsInvalidRequestWhenNoSignalingUriPresent()
        {
            // Read the appsettings json
            this.SetupConfiguration();

            var batchService = new BatchService(this.configuration);
            var controller = new Cloud3DSTKApiController(batchService);

            // Create a json body with no signaling uri
            var jsonBody = new CreateApiJsonBody
            {
                SignalingServerPort = 80
            };
            
            var result = await controller.Post((JObject)JToken.FromObject(jsonBody));

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.IsTrue((result as ObjectResult).Value.ToString().Equals(ApiResultMessages.ErrorNoSignalingFound));
        }

        /// <summary>
        /// Unit test for 0 dedicated nodes in the post body
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        public async Task CreateApiReturnsInvalidRequestWhenNoDedicatedNodes()
        {
            // Read the appsettings json
            this.SetupConfiguration();

            var batchService = new BatchService(this.configuration);
            var controller = new Cloud3DSTKApiController(batchService);

            // Create a json body with no signaling uri
            var jsonBody = new CreateApiJsonBody
            {
                SignalingServer = "http://test.com",
                SignalingServerPort = 80,
                DedicatedRenderingNodes = 0
            };

            var result = await controller.Post((JObject)JToken.FromObject(jsonBody));

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.IsTrue((result as ObjectResult).Value.ToString().Equals(ApiResultMessages.ErrorOneDedicatedNodeRequired));
        }

        /// <summary>
        /// Unit test for 0 max users per rendering node in the post body
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        public async Task CreateApiReturnsInvalidRequestWhenMaxUsersIs0()
        {
            // Read the appsettings json
            this.SetupConfiguration();

            var batchService = new BatchService(this.configuration);
            var controller = new Cloud3DSTKApiController(batchService);

            // Create a json body with no signaling uri
            var jsonBody = new CreateApiJsonBody
            {
                SignalingServer = "http://test.com",
                SignalingServerPort = 80,
                DedicatedRenderingNodes = 6,
                DedicatedTurnNodes = 1,
                MaxUsersPerRenderingNode = 0
            };

            var result = await controller.Post((JObject)JToken.FromObject(jsonBody));

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.IsTrue((result as ObjectResult).Value.ToString().Equals(ApiResultMessages.ErrorOneMaxUserRequired));
        }
        
        /// <summary>
        /// Private method to setup the configuration file for batch
        /// </summary>
        private void SetupConfiguration()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            this.configuration = configurationBuilder.Build();
        }
    }
}
