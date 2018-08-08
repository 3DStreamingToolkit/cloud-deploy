// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Tests.Models
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestratorApiJsonBody" /> class
    /// </summary>
    [Serializable]
    public class OrchestratorApiJsonBody
    {
        /// <summary>
        /// Gets or sets the total current sessions
        /// </summary>
        [JsonProperty("totalSessions")]
        public int TotalSessions { get; set; }

        /// <summary>
        /// Gets or sets the total current sessions
        /// </summary>
        [JsonProperty("totalSlots")]
        public int TotalSlots { get; set; }
    }
}
