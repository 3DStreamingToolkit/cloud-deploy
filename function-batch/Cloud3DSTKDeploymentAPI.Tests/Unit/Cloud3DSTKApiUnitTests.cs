// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Tests
{
    using System.Threading.Tasks;
    using Cloud3DSTKDeploymentAPI.Controllers;
    using Cloud3DSTKDeploymentAPI.Models;
    using Cloud3DSTKDeploymentAPI.Services;
    using Cloud3DSTKDeploymentAPI.Tests.Helpers;
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
        /// Unit test for no body post
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        public async Task CreateApiReturnsInvalidRequestWhenNoBody()
        {  
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            var controller = new Cloud3DSTKController(batchService);

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
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            var controller = new Cloud3DSTKController(batchService);

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
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            var controller = new Cloud3DSTKController(batchService);

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
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            var controller = new Cloud3DSTKController(batchService);

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
        /// Unit test for no body in delete pool api
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        public async Task DeletePoolApiReturnsInvalidRequestWhenNoBody()
        {
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            var controller = new Cloud3DSTKController(batchService);

            var result = await controller.DeletePool(null);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.IsTrue((result as ObjectResult).Value.ToString().Equals(ApiResultMessages.ErrorBodyIsEmpty));
        }

        /// <summary>
        /// Unit test for no body in delete pool api
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        public async Task DeletePoolApiReturnsInvalidRequestWhenPoolIdIsMissing()
        {
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            var controller = new Cloud3DSTKController(batchService);

            // Create a json body with no pool id
            var jsonBody = new DeletePoolApiJsonBody
            {
            };

            var result = await controller.DeletePool((JObject)JToken.FromObject(jsonBody));

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            Assert.IsTrue((result as ObjectResult).Value.ToString().Equals(ApiResultMessages.ErrorPoolIdRequired));
        }
    }
}
