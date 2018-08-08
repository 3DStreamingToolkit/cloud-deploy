// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Tests.Helpers
{
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Hosting;
    using Cloud3DSTKDeployment.Controllers;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Extensions to the Cloud3DSTKController 
    /// </summary>
    public class ControllerExtensions
    {
        /// <summary>
        /// Static method to return a new controller with http request
        /// </summary>
        /// <param name="configuration">Custom configuration for Batch service</param>
        /// <returns>Returns a Cloud3DSTKController</returns>
        public static Cloud3DSTKController NewCloudController(IConfiguration configuration = null)
        {
            var controller = configuration == null ? new Cloud3DSTKController() : new Cloud3DSTKController(configuration);

            controller.Request = new HttpRequestMessage();
            controller.Request.Properties.Add(
                HttpPropertyKeys.HttpConfigurationKey,
                new HttpConfiguration());

            return controller;
        }

        /// <summary>
        /// Static method to return a new controller with http request
        /// </summary>
        /// <param name="configuration">Custom configuration for Batch service</param>
        /// <returns>Returns a Orchestrator3DSTKController</returns>
        public static Orchestrator3DSTKController NewOrchestratorController(IConfiguration configuration = null)
        {
            var controller = configuration == null ? new Orchestrator3DSTKController() : new Orchestrator3DSTKController(configuration);

            controller.Request = new HttpRequestMessage();
            controller.Request.Properties.Add(
                HttpPropertyKeys.HttpConfigurationKey,
                new HttpConfiguration());

            return controller;
        }
    }
}
