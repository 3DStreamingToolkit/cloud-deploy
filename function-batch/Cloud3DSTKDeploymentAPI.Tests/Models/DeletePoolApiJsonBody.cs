// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Tests.Models
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeletePoolApiJsonBody" /> class
    /// </summary>
    [Serializable]
    public class DeletePoolApiJsonBody
    {
        /// <summary>
        /// Gets or sets the pool id to be deleted
        /// </summary>
        [JsonProperty("poolId")]
        public string PoolId { get; set; }
    }
}
