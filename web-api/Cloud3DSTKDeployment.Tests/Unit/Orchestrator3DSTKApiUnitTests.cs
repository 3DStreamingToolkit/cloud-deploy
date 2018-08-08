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
    public class Orchestrator3DSTKApiUnitTests
    {
        /// <summary>
        /// Unit test for no body in orchestrator api
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task OrchestratorReturnsInvalidRequestWhenNoBody()
        {
            var controller = ControllerExtensions.NewOrchestratorController();

            var result = await controller.Post(null);

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue(((ObjectContent)(result as HttpResponseMessage).Content).Value.ToString().Equals(ApiResultMessages.ErrorBodyIsEmpty));
        }

        /// <summary>
        /// Unit test for no body in orchestrator api
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task OrchestratorReturnsInvalidRequestWhenNoTotalSessions()
        {
            var controller = ControllerExtensions.NewOrchestratorController();

            // Create a json body with no TotalSessions
            var jsonBody = new OrchestratorApiJsonBody
            {
            };

            var result = await controller.Post((JObject)JToken.FromObject(jsonBody));

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue((result as HttpResponseMessage).StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Unit test for no body in orchestrator api
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(1)]
        public async Task OrchestratorReturnsNoAutoscalingWhenNotEnabled()
        {
            var configuration = ConfigurationHelper.GetConfiguration();
            configuration["AutomaticScalingThreshold"] = "0";

            var controller = ControllerExtensions.NewOrchestratorController(configuration);

            // Create a json body with correct parameters
            var jsonBody = new OrchestratorApiJsonBody
            {
                TotalSessions = 5,
                TotalSlots = 10
            };

            var result = await controller.Post((JObject)JToken.FromObject(jsonBody));

            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue((result as HttpResponseMessage).StatusCode == System.Net.HttpStatusCode.BadRequest);
            Assert.IsTrue(((ObjectContent)(result as HttpResponseMessage).Content).Value.ToString().Equals(ApiResultMessages.WarningNoAutoscaling));
        }
    }
}
