// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Models
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoscalingStatus" /> enum
    /// </summary>
    public enum AutoscalingStatus
    {
        /// <summary>
        /// Returned when the up scaling threshold is met and a new rendering pool is needed
        /// </summary>
        UpscaleRenderingPool,

        /// <summary>
        /// Returned when the down scaling threshold is met and a rendering pool should be removed
        /// </summary>
        DownscaleRenderingPool,

        /// <summary>
        /// Returned when the up scaling threshold is met and a new turn pool is needed
        /// </summary>
        UpscaleTurnPool,

        /// <summary>
        /// Returned when the down scaling threshold is met and a turn pool should be removed
        /// </summary>
        DownscaleTurnPool,

        /// <summary>
        /// Scaling capacity is within thresholds, no action is needed
        /// </summary>
        OK,

        /// <summary>
        /// Auto scaling is not enabled
        /// </summary>
        NotEnabled
    }
}