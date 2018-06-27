// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Helpers
{
    using Microsoft.Extensions.Configuration;
    
    /// <summary>
    /// Helper class to retrieve the appsettings configuration file
    /// </summary>
    public class ConfigurationHelper
    {   
        /// <summary>
        /// Gets or sets the private configuration interface 
        /// </summary>
        private static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets the interface to the appsettings configuration file
        /// </summary>
        /// <returns>Return the configuration interface</returns>
        public static IConfiguration GetConfiguration()
        {
            if (Configuration == null)
            {
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddJsonFile("appsettings.json");
                Configuration = configurationBuilder.Build();
                return Configuration;
            }
            else
            {
                return Configuration;
            }
        }
    }
}
