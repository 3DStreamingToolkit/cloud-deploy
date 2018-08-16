// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeployment.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloud3DSTKDeployment.Models;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Auth;
    using Microsoft.Azure.Batch.Common;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Batch service for 3DSTK scaling
    /// </summary>
    public class BatchService : IBatchService
    {
        /// <summary>
        /// Server path inside each VM for the streaming application
        /// </summary>
        private readonly string serverPath = "C:/3dstk/Server";

        /// <summary>
        /// Number of dedicated TURN nodes per Batch account
        /// </summary>
        private int dedicatedTurnNodes = 1;

        /// <summary>
        /// Number of dedicated rendering nodes per Batch pool
        /// </summary>
        private int dedicatedRenderingNodes = 1;

        /// <summary>
        /// Max number of clients per rendering VM
        /// </summary>
        private int maxUsersPerRenderingNode = 1;

        /// <summary>
        /// The threshold for automatic creation of new rendering pools
        /// </summary>
        private int automaticScalingUpThreshold = 0;

        /// <summary>
        /// The threshold for automatic scaling down of rendering pools
        /// </summary>
        private int automaticScalingDownThreshold = 0;

        /// <summary>
        /// The minimum number of rendering pools to keep alive
        /// </summary>
        private int minimumRenderingPools = int.MaxValue;

        /// <summary>
        /// The vnet for all batch pools
        /// </summary>
        private string vnet;

        /// <summary>
        /// The signaling server url
        /// </summary>
        private string signalingServerUrl;

        /// <summary>
        /// The signaling server port
        /// </summary>
        private int signalingServerPort = int.MaxValue;

        /// <summary>
        /// Batch client private instance
        /// </summary>
        private BatchClient batchClient;

        /// <summary>
        /// Local instance of all rendering pools
        /// </summary>
        private IEnumerable<CloudPool> currentRenderingPools;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchService" /> class
        /// </summary>
        /// <param name="configuration">Configuration for the appsettings file</param>
        public BatchService(IConfiguration configuration)
        {
            Configuration = configuration;

            var batchAccountName = Configuration["BatchAccountName"];
            var batchAccountKey = Configuration["BatchAccountKey"];
            var batchAccountUrl = Configuration["BatchAccountUrl"];

            int.TryParse(Configuration["DedicatedTurnNodes"], out this.dedicatedTurnNodes);
            int.TryParse(Configuration["DedicatedRenderingNodes"], out this.dedicatedRenderingNodes);
            int.TryParse(Configuration["MaxUsersPerRenderingNode"], out this.maxUsersPerRenderingNode);
            int.TryParse(Configuration["AutomaticScalingUpThreshold"], out this.automaticScalingUpThreshold);
            int.TryParse(Configuration["AutomaticScalingDownThreshold"], out this.automaticScalingDownThreshold);
            int.TryParse(Configuration["MinimumRenderingPools"], out this.minimumRenderingPools);
            int.TryParse(Configuration["SignalingServerPort"], out this.signalingServerPort);
            int.TryParse(Configuration["SignalingServerPort"], out this.signalingServerPort);

            this.signalingServerUrl = Configuration["SignalingServerUrl"];
            this.vnet = Configuration["Vnet"];

            // Use shared credentials. This will not work with vnet and custom images
            BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(batchAccountUrl, batchAccountName, batchAccountKey);
            this.batchClient = BatchClient.Open(cred);

            // Use token credentials. This will give you full access to the batch api.
            // var authorityUri = Configuration["AuthorityUri"];
            // var batchResourceUri = Configuration["BatchResourceUri"];
            // var clientId = Configuration["ClientId"];
            // var redirectUri = Configuration["RedirectUri"];

            // Task.Run(async () =>
            // {
            //    Task<string> tokenProvider() => this.GetAuthenticationTokenAsync(authorityUri, batchResourceUri, clientId, redirectUri);
            //    this.batchClient = await BatchClient.OpenAsync(new BatchTokenCredentials(batchAccountUrl, tokenProvider));
            // }).Wait();
        }

        /// <summary>
        /// Gets or sets the interface to the appsettings configuration file
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Returns a list of all pools in the batch service
        /// </summary>
        /// <returns>A list of CloudPool</returns>
        public IList<CloudPool> GetPoolsInBatch()
        {
            var poolData = this.batchClient.PoolOperations.ListPools();
            return poolData.ToList();
        }

        /// <summary>
        /// Method to return maximum capacity of rendering slots, including pending pools
        /// </summary>
        /// <returns>Returns the max rendering slots capacity</returns>
        public int GetMaxRenderingSlotsCapacity()
        {
            var allPools = this.GetPoolsInBatch();
            this.currentRenderingPools = allPools.Where(i => i.VirtualMachineConfiguration.ImageReference.Offer.Equals("WindowsServer") &&
                                                    i.State != PoolState.Deleting &&
                                                    i.TargetDedicatedComputeNodes.HasValue &&
                                                    i.TargetDedicatedComputeNodes.Value > 0);
            int totalRenderingNodes = 0;

            foreach (var pool in this.currentRenderingPools)
            {
                totalRenderingNodes += pool.TargetDedicatedComputeNodes.Value;
            }

            return totalRenderingNodes * this.maxUsersPerRenderingNode;
        }

        /// <summary>
        /// Method to return if auto scaling up or down is enabled
        /// </summary>
        /// <returns>Returns if auto scale is enabled</returns>
        public bool IsAutoScaling()
        {
            return this.automaticScalingUpThreshold > 0 || this.automaticScalingDownThreshold > 0;
        }

        /// <summary>
        /// Method to return if the batch client is approaching rendering capacity
        /// </summary>
        /// <param name="totalClients">Total number of connected clients</param>
        /// <param name="renderingServers">A list of rendering servers connected to the signaling server</param>
        /// <param name="deletePoolId">The pool id to be removed, used only for downscaling</param>
        /// <returns>Returns true/false if we are approaching rendering capacity</returns>
        public AutoscalingStatus GetAutoscalingStatus(int totalClients, List<ConnectedServer> renderingServers, out string deletePoolId)
        {
            deletePoolId = string.Empty;
            if (!this.IsAutoScaling())
            {
                return AutoscalingStatus.NotEnabled;
            }

            var currentCapacity = this.GetMaxRenderingSlotsCapacity();
            if (currentCapacity < 1)
            {
                // We have no pools
                return AutoscalingStatus.UpscaleRenderingPool;
            }

            var currentSlotsUsagePercentage = (float)totalClients / currentCapacity * 100;
            if (currentSlotsUsagePercentage > this.automaticScalingUpThreshold)
            {
                // We are approaching capacity, we need a new pool
                return AutoscalingStatus.UpscaleRenderingPool;
            }
            else if (currentSlotsUsagePercentage < this.automaticScalingDownThreshold)
            {
                // Down scaling only looks at stable pools that have idle compute nodes
                var idleRenderingPools = this.currentRenderingPools.Where(i => i.AllocationState == AllocationState.Steady && i.ListComputeNodes().All(c => c.State == ComputeNodeState.Idle));
                if (idleRenderingPools.Count() > this.minimumRenderingPools)
                {
                    // Check if any of the nodes are connected to the signaling server
                    foreach (var pool in this.currentRenderingPools)
                    {
                        bool poolIsIdle = true;

                        foreach (var node in pool.ListComputeNodes())
                        {
                            if (renderingServers.Any(s => s.Ip == node.GetRemoteLoginSettings().IPAddress))
                            {
                                poolIsIdle = false;
                                break;
                            }
                        }

                        if (poolIsIdle)
                        {
                            deletePoolId = pool.Id;
                            return AutoscalingStatus.DownscaleRenderingPool;
                        }
                    }
                }
            }

            return AutoscalingStatus.OK;
        }

        /// <summary>
        /// Method to return if the api has a valid configuration
        /// </summary>
        /// <returns>Returns an empty string for a valid configuration or an error message if not</returns>
        public string HasValidConfiguration()
        {
            // Signaling URL and port is required
            if (string.IsNullOrEmpty(this.signalingServerUrl) || this.signalingServerPort == int.MaxValue)
            {
                return ApiResultMessages.ErrorNoSignalingFound;
            }

            // Each pool must have at least one dedicated node
            if (this.dedicatedRenderingNodes < 1 || this.dedicatedTurnNodes < 1)
            {
                return ApiResultMessages.ErrorOneDedicatedNodeRequired;
            }

            // Each rendering node must have at least one max user
            if (this.maxUsersPerRenderingNode < 1)
            {
                return ApiResultMessages.ErrorOneMaxUserRequired;
            }

            return string.Empty;
        }

        /// <summary>
        /// Resizes a TURN server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be resized</param>
        /// <param name="dedicatedNodes">The new number of dedicated nodes inside the pool</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<bool> ResizeTurnPool(string poolId, int dedicatedNodes)
        {
            var pool = await this.batchClient.PoolOperations.GetPoolAsync(poolId);
            if (pool == null)
            {
                return false;
            }

            if (pool.CurrentDedicatedComputeNodes == dedicatedNodes)
            {
                return false;
            }

            await pool.ResizeAsync(dedicatedNodes, 0, TimeSpan.FromMinutes(30));
            return true;
        }

        /// <summary>
        /// Creates a TURN server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be created</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<object> CreateTurnPool(string poolId)
        {
            CloudPool pool;
            try
            {
                Console.WriteLine("Creating Linux pool [{0}]...", poolId);

                // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
                // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
                pool = this.batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: this.dedicatedTurnNodes,
                    virtualMachineSize: "STANDARD_A1",
                    virtualMachineConfiguration: new VirtualMachineConfiguration(
                        new ImageReference(
                            offer: "UbuntuServer",
                            publisher: "Canonical",
                            sku: "14.04.5-LTS"),
                        nodeAgentSkuId: "batch.node.ubuntu 14.04"));

                // Create and assign the StartTask that will be executed when compute nodes join the pool.
                pool.StartTask = new StartTask
                {
                    // Run a command to install docker and get latest TURN server implementation
                    CommandLine = "bash -c \"sudo apt-get update && sudo apt-get -y install docker.io && sudo docker run -d -p 3478:3478 -p 3478:3478/udp --restart=always zolochevska/turn-server username password realm\"",
                    UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.Admin)),
                    WaitForSuccess = true,
                    MaxTaskRetryCount = 2
                };

                pool.NetworkConfiguration = this.GetNetworkConfigurationForTURN(this.vnet);

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                var stringError = string.Empty;
                foreach (var value in be.RequestInformation.BatchError.Values)
                {
                    stringError += value.Value + " ";
                }

                return stringError;
            }

            return pool;
        }

        /// <summary>
        /// Method to wait until node creation is complete
        /// </summary>
        /// <param name="node">The compute node</param>
        /// <param name="desiredState">The desired state of the pool</param>
        /// <returns>Returns a boolean when the pool is ready</returns>
        public async Task<bool> AwaitDesiredNodeState(ComputeNode node, ComputeNodeState desiredState)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            watch.Start();

            do
            {
                // Refresh pool to get latest state
                await node.RefreshAsync();

                // Timeout after 30 minutes or if the node is an invalid state
                if (watch.ElapsedTicks > TimeSpan.TicksPerHour / 2 ||
                    node.State == ComputeNodeState.Unknown ||
                    node.State == ComputeNodeState.Unusable ||
                    node.State == ComputeNodeState.Offline ||
                    node.State == ComputeNodeState.StartTaskFailed ||
                    node.State == ComputeNodeState.Preempted)
                {
                    return false;
                }
                
                await Task.Delay(5000);
            }
            while (node.State != desiredState);

            return true;
        }

        /// <summary>
        /// Method to wait until desired pool state is achieved
        /// </summary>
        /// <param name="pool">The pool</param>
        /// <param name="desiredState">The desired state of the pool</param>
        /// <returns>Returns a boolean when the pool is ready</returns>
        public async Task<bool> AwaitDesiredPoolState(CloudPool pool, AllocationState desiredState)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            watch.Start();

            do
            {
                await Task.Delay(5000);

                // Refresh pool to get latest state
                await pool.RefreshAsync();

                // Timeout after 30 minutes
                if (watch.ElapsedTicks > TimeSpan.TicksPerHour / 2)
                {
                    return false;
                }
            }
            while (pool.AllocationState != desiredState);

            return true;
        }

        /// <summary>
        /// Creates a rendering server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be created</param>
        /// <param name="turnServerIp">The IP of the desired TURN server</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<object> CreateRenderingPool(string poolId, string turnServerIp)
        {
            CloudPool pool;
            try
            {
                Console.WriteLine("Creating pool [{0}]...", poolId);

                // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
                // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
                pool = this.batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: this.dedicatedRenderingNodes,
                    virtualMachineSize: "Standard_NV6",  // NV-series, 6 CPU, 1 GPU, 56 GB RAM 
                    virtualMachineConfiguration: new VirtualMachineConfiguration(
                        new ImageReference(
                            offer: "WindowsServer",
                            publisher: "MicrosoftWindowsServer",
                            sku: "2016-DataCenter",
                            version: "latest"),
                        nodeAgentSkuId: "batch.node.windows amd64"));

                pool.MaxTasksPerComputeNode = 1;
                pool.TaskSchedulingPolicy = new TaskSchedulingPolicy(ComputeNodeFillType.Spread);

                if (!string.IsNullOrWhiteSpace(this.vnet))
                {
                    pool.NetworkConfiguration = new NetworkConfiguration
                    {
                        SubnetId = this.vnet
                    };
                }
                
                // Command to start the rendering service
                var startRenderingCommand = string.Format(
                    "cmd /c powershell -command \"start-process powershell -verb runAs -ArgumentList '-ExecutionPolicy Unrestricted -file %AZ_BATCH_APP_PACKAGE_server-deploy-script#1.0%\\server_deploy.ps1 {1} {2} {3} {4} {5} {6} {7} {0} '\"",
                    this.serverPath,
                    string.Format("turn:{0}:3478", turnServerIp),
                    "username",
                    "password",
                    this.signalingServerUrl,
                    this.signalingServerPort,
                    5000,
                    this.maxUsersPerRenderingNode);

                // Create and assign the StartTask that will be executed when compute nodes join the pool.
                // In this case, we copy the StartTask's resource files (that will be automatically downloaded
                // to the node by the StartTask) into the shared directory that all tasks will have access to.
                pool.StartTask = new StartTask
                {
                    // Install all packages and run Unit tests to ensure the node is ready for streaming
                    CommandLine = string.Format(
                        "cmd /c robocopy %AZ_BATCH_APP_PACKAGE_sample-server#1.0% {0} /E && " +
                        "cmd /c %AZ_BATCH_APP_PACKAGE_vc-redist#2015%\\vc_redist.x64.exe /install /passive /norestart && " +
                        "cmd /c %AZ_BATCH_APP_PACKAGE_NVIDIA#391.58%\\setup.exe /s && " +
                        "cmd /c %AZ_BATCH_APP_PACKAGE_native-server-tests#1%\\NativeServerTests\\NativeServer.Tests.exe --gtest_also_run_disabled_tests --gtest_filter=\"*Driver*:*Hardware*\" &&" +
                        startRenderingCommand,
                    this.serverPath),
                    UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.Admin)),
                    WaitForSuccess = true,
                    MaxTaskRetryCount = 2
                };

                // Specify the application and version to install on the compute nodes
                pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                    {
                        new ApplicationPackageReference
                        {
                            ApplicationId = "NVIDIA",
                            Version = "391.58"
                        },
                        new ApplicationPackageReference
                        {
                            ApplicationId = "native-server-tests",
                            Version = "1"
                        },
                        new ApplicationPackageReference
                        {
                            ApplicationId = "sample-server",
                            Version = "1.0"
                        },
                         new ApplicationPackageReference
                         {
                            ApplicationId = "vc-redist",
                            Version = "2015"
                         },
                         new ApplicationPackageReference
                         {
                            ApplicationId = "server-deploy-script",
                            Version = "1.0"
                         }
                    };

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                var stringError = string.Empty;
                foreach (var value in be.RequestInformation.BatchError.Values)
                {
                    stringError += value.Value + " ";
                }

                return stringError;
            }

            return pool;
        }
        
        /// <summary>
        /// Delete a specific pool
        /// </summary>
        /// <param name="poolId">The pool id to be deleted</param>
        /// <returns>Return a task that can be awaited</returns>
        public async Task DeletePoolAsync(string poolId)
        {
            await this.batchClient.PoolOperations.DeletePoolAsync(poolId);
        }

        /// <summary>
        /// Gets the TURN firewall rules to enable specific UDP ports
        /// </summary>
        /// <param name="vnet">The vnet for each node</param>
        /// <returns>Returns the network configuration</returns>
        private NetworkConfiguration GetNetworkConfigurationForTURN(string vnet)
        {
            var networkConfiguration = new NetworkConfiguration
            {
                EndpointConfiguration = new PoolEndpointConfiguration(new InboundNatPool[]
                   {
                    new InboundNatPool(
                        name: "UDP_3478",
                        protocol: InboundEndpointProtocol.Udp,
                        backendPort: 3478,
                        frontendPortRangeStart: 3478,
                        frontendPortRangeEnd: 3578),
                    new InboundNatPool(
                        name: "UDP_5349",
                        protocol: InboundEndpointProtocol.Udp,
                        backendPort: 5349,
                        frontendPortRangeStart: 5349,
                        frontendPortRangeEnd: 5449),
                   }.ToList())
            };
            
            if (!string.IsNullOrWhiteSpace(vnet))
            {
                networkConfiguration.SubnetId = vnet;
            }

            return networkConfiguration;
        }

        /// <summary>
        /// Acquire the authentication token from Azure AD.
        /// </summary>
        /// <param name="authorityUri">The AD authority uri</param>
        /// <param name="batchResourceUri">The batch resource uri</param>
        /// <param name="clientId">The batch client id</param>
        /// <param name="redirectUri">The redirect uri of the AD application</param>
        /// <returns>Return the Azure AD authentication token</returns>
        private async Task<string> GetAuthenticationTokenAsync(string authorityUri, string batchResourceUri, string clientId, string redirectUri)
        {
            var authContext = new AuthenticationContext(authorityUri);

            var authResult = await authContext.AcquireTokenAsync(
                                                    batchResourceUri,
                                                    clientId,
                                                    new Uri(redirectUri), 
                                                    new PlatformParameters(PromptBehavior.Auto));

            return authResult.AccessToken;
        }
    }
}
