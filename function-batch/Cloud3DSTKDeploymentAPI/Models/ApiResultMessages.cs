// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Models
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiErrors" /> class
    /// </summary>
    public class ApiResultMessages
    {
        /// <summary>
        /// Gets the success string message for the batch api
        /// </summary>
        public static string SuccessMessage { get; } = "All tasks reached state Completed";

        /// <summary>
        /// Gets the failure message for the batch api
        /// </summary>
        public static string FailureMessage { get; } = "One or more tasks failed to reach the Completed state within the timeout period";

        /// <summary>
        /// Gets the string error when no body was found
        /// </summary>
        public static string ErrorBodyIsEmpty { get; } = "A json body is required";

        /// <summary>
        /// Gets the string error when no signaling uri or port is found
        /// </summary>
        public static string ErrorNoSignalingFound { get; } = "Signaling is required";

        /// <summary>
        /// Gets the string error when no dedicated nodes are found
        /// </summary>
        public static string ErrorOneDedicatedNodeRequired { get; } = "Pools must have at least one dedicated node";

        /// <summary>
        /// Gets the string error when no max users are specified
        /// </summary>
        public static string ErrorOneMaxUserRequired { get; } = "Rendering nodes must have at least one max user";
    }
}
