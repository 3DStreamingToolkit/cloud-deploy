// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloud3DSTKDeployment.Helpers;
    using Cloud3DSTKDeployment.Models;
    using Cloud3DSTKDeployment.Tests.Helpers;
    using Cloud3DSTKDeployment.Tests.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// The unit tests for the 3DSTK cloud scaling api
    /// </summary>
    [TestClass]
    public class Cloud3DSTKApiUnitTests
    {
        /// <summary>
        /// Unit test for no signaling uri or port in configuration
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task CreateReturnsInvalidRequestWhenNoSignalingUriPresent()
        {
            var configuration = ConfigurationHelper.GetConfiguration();
            configuration["SignalingServerPort"] = null;
            configuration["SignalingServerUrl"] = string.Empty;

            var controller = ControllerExtensions.NewCloudController(configuration);
            var result = await controller.Post(null);

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue(((ObjectContent)(result as HttpResponseMessage).Content).Value.ToString().Equals(ApiResultMessages.ErrorNoSignalingFound));
        }

        /// <summary>
        /// Unit test for 0 dedicated nodes in configuration
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task CreateReturnsInvalidRequestWhenNoDedicatedNodes()
        {
            var configuration = ConfigurationHelper.GetConfiguration();
            configuration["SignalingServerPort"] = "80";
            configuration["SignalingServerUrl"] = "http://www.signaling-url.com";
            configuration["DedicatedRenderingNodes"] = "0";

            var controller = ControllerExtensions.NewCloudController(configuration);
            var result = await controller.Post(null);

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue(((ObjectContent)(result as HttpResponseMessage).Content).Value.ToString().Equals(ApiResultMessages.ErrorOneDedicatedNodeRequired));
        }

        /// <summary>
        /// Unit test for 0 max users per rendering node in configuration
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task CreateReturnsInvalidRequestWhenMaxUsersIs0()
        {
            var configuration = ConfigurationHelper.GetConfiguration();
            configuration["SignalingServerPort"] = "80";
            configuration["SignalingServerUrl"] = "http://www.signaling-url.com";
            configuration["MaxUsersPerRenderingNode"] = "0";

            var controller = ControllerExtensions.NewCloudController(configuration);
            var result = await controller.Post(null);

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue(((ObjectContent)(result as HttpResponseMessage).Content).Value.ToString().Equals(ApiResultMessages.ErrorOneMaxUserRequired));
        }

        /// <summary>
        /// Unit test for no body in delete pool api
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task DeletePoolReturnsInvalidRequestWhenNoBody()
        {
            var controller = ControllerExtensions.NewCloudController();

            var result = await controller.DeletePool(null);

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue(((ObjectContent)(result as HttpResponseMessage).Content).Value.ToString().Equals(ApiResultMessages.ErrorBodyIsEmpty));
        }

        /// <summary>
        /// Unit test for no body in delete pool api
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task DeletePoolReturnsInvalidRequestWhenPoolIdIsMissing()
        {
            var controller = ControllerExtensions.NewCloudController();

            // Create a json body with no pool id
            var jsonBody = new DeletePoolApiJsonBody
            {
            };

            var result = await controller.DeletePool((JObject)JToken.FromObject(jsonBody));

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue(((ObjectContent)(result as HttpResponseMessage).Content).Value.ToString().Equals(ApiResultMessages.ErrorPoolIdRequired));
        }
    }
}
