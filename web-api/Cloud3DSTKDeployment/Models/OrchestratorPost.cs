// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Models
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedServer" /> class
    /// </summary>
    [Serializable]
    public class ConnectedServer
    {
        /// <summary>
        /// Gets or sets the number of slots on the server
        /// </summary>
        [JsonProperty("slots")]
        public int Slots { get; set; }

        /// <summary>
        /// Gets or sets the server IP address
        /// </summary>
        [JsonProperty("ip")]
        public string Ip { get; set; }
    }
}