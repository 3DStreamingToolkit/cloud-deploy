// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Tests.Helpers
{
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Hosting;
    using Cloud3DSTKDeployment.Controllers;
    
    /// <summary>
    /// Extensions to the Cloud3DSTKController 
    /// </summary>
    public class ControllerExtensions
    {
        /// <summary>
        /// Static method to return a new controller with http request
        /// </summary>
        /// <returns>Returns a Cloud3DSTKController</returns>
        public static Cloud3DSTKController NewController()
        {
            var controller = new Cloud3DSTKController
            {
                Request = new HttpRequestMessage()
            };

            controller.Request.Properties.Add(
                HttpPropertyKeys.HttpConfigurationKey,
                new HttpConfiguration());

            return controller;
        }
    }
}
