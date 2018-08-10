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
        [JsonProperty("id")]
        public Server Server { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Server" /> class
    /// </summary>
    [Serializable]
    public class Server
    {
        [JsonProperty("slots")]
        public int Slots { get; set; }
        [JsonProperty("ip")]
        public string Ip { get; set; }
    }
}