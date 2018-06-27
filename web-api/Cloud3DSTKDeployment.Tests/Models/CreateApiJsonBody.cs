// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Tests.Models
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
        /// Gets or sets the signalingServer field
        /// </summary>
        [JsonProperty("signalingServer")]
        public string SignalingServer { get; set; }

        /// <summary>
        /// Gets or sets the signalingServerPort field
        /// </summary>
        [JsonProperty("signalingServerPort")]
        public int SignalingServerPort { get; set; }

        /// <summary>
        /// Gets or sets the vnet
        /// </summary>
        [JsonProperty("vnet")]
        public string Vnet { get; set; }

        /// <summary>
        /// Gets or sets the renderingPoolId field
        /// </summary>
        [JsonProperty("renderingPoolId")]
        public string RenderingPoolId { get; set; }

        /// <summary>
        /// Gets or sets the renderingJobId field
        /// </summary>
        [JsonProperty("renderingJobId")]
        public string RenderingJobId { get; set; }

        /// <summary>
        /// Gets or sets the dedicatedRenderingNodes field
        /// </summary>
        [JsonProperty("dedicatedRenderingNodes")]
        public int DedicatedRenderingNodes { get; set; }

        /// <summary>
        /// Gets or sets the maxUsersPerRenderingNode field
        /// </summary>
        [JsonProperty("maxUsersPerRenderingNode")]
        public int MaxUsersPerRenderingNode { get; set; }

        /// <summary>
        /// Gets or sets the turnPoolId field
        /// </summary>
        [JsonProperty("turnPoolId")]
        public string TurnPoolId { get; set; }

        /// <summary>
        /// Gets or sets the dedicatedTurnNodes field
        /// </summary>
        [JsonProperty("dedicatedTurnNodes")]
        public int DedicatedTurnNodes { get; set; }
    }
}
