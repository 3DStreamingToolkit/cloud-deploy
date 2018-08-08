// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Tests.Functional
{
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloud3DSTKDeployment.Helpers;
    using Cloud3DSTKDeployment.Models;
    using Cloud3DSTKDeployment.Services;
    using Cloud3DSTKDeployment.Tests.Helpers;
    using Cloud3DSTKDeployment.Tests.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// The functional unit tests for the 3DSTK cloud scaling api
    /// </summary>
    [TestClass]
    public class Cloud3DSTKApiFunctionalTests
    {
        /// <summary>
        /// End to end functional test to spin up a rendering and TURN pool inside the batch account
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(2)]
        public async Task FunctionalEndToEndCreateTest()
        {
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            Assert.IsTrue(string.IsNullOrWhiteSpace(batchService.HasValidConfiguration()));
            
            var controller = ControllerExtensions.NewCloudController();

            // Create a fully functional json body
            var jsonBody = new CreateApiJsonBody
            {
                RenderingJobId = "TEST_JOB",
                TurnPoolId = "TEST_TURN",
                RenderingPoolId = "TEST_RENDERING"
            };

            var result = await controller.Post((JObject)JToken.FromObject(jsonBody));
            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));

            var pool = batchService.GetPoolsInBatch();

            Assert.IsNotNull(pool.FirstOrDefault(p => p.Id == jsonBody.TurnPoolId));
            Assert.IsNotNull(pool.FirstOrDefault(p => p.Id == jsonBody.RenderingPoolId));
        }

        /// <summary>
        /// Functional test to delete a pool from the batch client
        /// </summary>
        /// <returns>The result of the test</returns>
        [TestMethod]
        [Priority(2)]
        public async Task FunctionalDeletePoolIdTest()
        {
            var batchService = new BatchService(ConfigurationHelper.GetConfiguration());
            var controller = ControllerExtensions.NewCloudController();

            // Create a json body with a specific pool id
            var jsonBody = new DeletePoolApiJsonBody
            {
                PoolId = "TEST_RENDERING"
            };

            var result = await controller.DeletePool((JObject)JToken.FromObject(jsonBody));
            Assert.IsInstanceOfType(result, typeof(HttpResponseMessage));
            Assert.IsTrue(result.IsSuccessStatusCode);

            var pool = batchService.GetPoolsInBatch();
            Assert.IsTrue(pool?.FirstOrDefault(p => p.Id == jsonBody.PoolId).State == Microsoft.Azure.Batch.Common.PoolState.Deleting);
        }
    }
}
