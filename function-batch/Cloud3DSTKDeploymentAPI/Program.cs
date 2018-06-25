// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// The main program class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point to the application
        /// </summary>
        /// <param name="args">All the params passed into the main application</param>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates an instance of the IWebHostBuilder
        /// </summary>
        /// <param name="args">All the args passed from main</param>
        /// <returns>Returns an instance of the IWebHostBuilder</returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
