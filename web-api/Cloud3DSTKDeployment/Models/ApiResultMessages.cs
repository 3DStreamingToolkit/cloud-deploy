// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Models
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
        /// Gets the string error when auto scaling is not enabled
        /// </summary>
        public static string WarningNoAutoscaling { get; } = "Auto scaling is not enabled";

        /// <summary>
        /// Gets the string error when no body was found
        /// </summary>
        public static string ErrorBodyIsEmpty { get; } = "A json body is required";

        /// <summary>
        /// Gets the string error when turn nodes are invalid
        /// </summary>
        public static string ErrorTurnServerInvalid { get; } = "The TURN server nodes are in an invalid state. Please check the batch client for errors.";
        
        /// <summary>
        /// Gets the string error when turn pool was not successful
        /// </summary>
        public static string ErrorToCreateTurnPool { get; } = "Creating a TURN pool was not successful. Please check the batch client for errors.";

        /// <summary>
        /// Gets the string error when create rendering pool was not successful
        /// </summary>
        public static string ErrorToCreateRenderingPool { get; } = "Creating a rendering pool was not successful. Please check the batch client for errors.";

        /// <summary>
        /// Gets the string error when rendering pool exists
        /// </summary>
        public static string ErrorRenderingPoolExists { get; } = "The rendering pool id already exists, use a different id.";
        
        /// <summary>
        /// Gets the string error when job exists
        /// </summary>
        public static string ErrorJobExists { get; } = "The job id already exists, use a different id.";
        
        /// <summary>
        /// Gets the string error when no signaling uri or port is found
        /// </summary>
        public static string ErrorNoSignalingFound { get; } = "Signaling is required";

        /// <summary>
        /// Gets the string error when no vnet is found
        /// </summary>
        public static string ErrorNoVnetFound { get; } = "Vnet is required";

        /// <summary>
        /// Gets the string error when the pool id was not found
        /// </summary>
        public static string ErrorPoolIdNotFound { get; } = "The specified poolId does not exist in the batch client";

        /// <summary>
        /// Gets the string error when no signaling uri or port is found
        /// </summary>
        public static string ErrorPoolIdRequired { get; } = "poolId is required for delete";

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
