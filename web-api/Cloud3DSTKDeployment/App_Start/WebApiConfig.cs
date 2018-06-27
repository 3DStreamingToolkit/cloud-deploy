// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment
{
    using System.Web.Http;

    /// <summary>
    /// Static class for the web config
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// Register the configuration
        /// </summary>
        /// <param name="config">The configuration</param>
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });
        }
    }
}
