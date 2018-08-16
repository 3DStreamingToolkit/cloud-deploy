// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Models
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateApiJsonBody" /> class
    /// </summary>
    [Serializable]
    public class CreateApiJsonBody
    {
        /// <summary>
        /// Gets or sets the renderingPoolId field
        /// </summary>
        [JsonProperty("renderingPoolId")]
        public string RenderingPoolId { get; set; }

        /// <summary>
        /// Gets or sets the turnPoolId field
        /// </summary>
        [JsonProperty("turnPoolId")]
        public string TurnPoolId { get; set; }
    }
}
