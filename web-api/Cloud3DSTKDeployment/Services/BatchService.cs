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
        private int automaticScalingThreshold = int.MaxValue;

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
            int.TryParse(Configuration["AutomaticScalingThreshold"], out this.automaticScalingThreshold);
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
            var totalRenderingPools = allPools.Where(i => i.VirtualMachineConfiguration.ImageReference.Offer.Equals("WindowsServer")).Count();
            
            return totalRenderingPools * this.dedicatedRenderingNodes * this.maxUsersPerRenderingNode;
        }

        /// <summary>
        /// Method to return if the batch client is approaching rendering capacity
        /// </summary>
        /// <param name="totalClients">The number of current clients</param>
        /// <returns>Returns true/false if we are approaching rendering capacity</returns>
        public bool ApproachingRenderingCapacity(int totalClients)
        {
            if (this.automaticScalingThreshold == int.MaxValue)
            {
                return false;
            }

            return totalClients / this.GetMaxRenderingSlotsCapacity() * 100 > this.automaticScalingThreshold;
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
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<object> CreateRenderingPool(string poolId)
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
                        "cmd /c %AZ_BATCH_APP_PACKAGE_native-server-tests#1%\\NativeServerTests\\NativeServer.Tests.exe --gtest_also_run_disabled_tests --gtest_filter=\"*Driver*:*Hardware*\"",
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
        /// Creates the rendering tasks for each node inside the pool
        /// The task runs a PowerShell script to update the signaling and TURN information inside each node
        /// </summary>
        /// <param name="turnServerIp">The TURN server public ip</param>
        /// <param name="jobId">The job id for the tasks</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<bool> AddRenderingTasksAsync(string turnServerIp, string jobId)
        {
            // Create a collection to hold the tasks that we'll be adding to the job
            List<CloudTask> tasks = new List<CloudTask>();

            var taskId = Guid.NewGuid().ToString();
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

            CloudTask task = new CloudTask(taskId, startRenderingCommand)
            {
                UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.Admin))
            };
            tasks.Add(task);

            Console.WriteLine("Adding {0} tasks to job [{1}]...", tasks.Count, jobId);

            // Add the tasks as a collection opposed to a separate AddTask call for each. Bulk task submission
            // helps to ensure efficient underlying API calls to the Batch service.
            await this.batchClient.JobOperations.AddTaskAsync(jobId, tasks);

            return true;
        }

        /// <summary>
        /// Monitors the specified tasks for completion and returns a value indicating whether all tasks completed successfully
        /// within the timeout period.
        /// </summary>
        /// <param name="jobId">The id of the job containing the tasks that should be monitored.</param>
        /// <param name="timeout">The period of time to wait for the tasks to reach the completed state.</param>
        /// <returns><c>true</c> if all tasks in the specified job completed with an exit code of 0 within the specified timeout period, otherwise <c>false</c>.</returns>
        public async Task<bool> MonitorTasks(string jobId, TimeSpan timeout)
        {
            bool allTasksSuccessful = true;

            // Obtain the collection of tasks currently managed by the job. Note that we use a detail level to
            // specify that only the "id" property of each task should be populated. Using a detail level for
            // all list operations helps to lower response time from the Batch service.
            ODATADetailLevel detail = new ODATADetailLevel(selectClause: "id");
            List<CloudTask> tasks = await this.batchClient.JobOperations.ListTasks(jobId, detail).ToListAsync();

            Console.WriteLine("Awaiting task completion, timeout in {0}...", timeout.ToString());
            
            // We use a TaskStateMonitor to monitor the state of our tasks. In this case, we will wait for all tasks to
            // reach the Completed state.
            TaskStateMonitor taskStateMonitor = this.batchClient.Utilities.CreateTaskStateMonitor();
            try
            {
                await taskStateMonitor.WhenAll(tasks, TaskState.Completed, timeout);
            }
            catch (TimeoutException)
            {
                await this.batchClient.JobOperations.TerminateJobAsync(jobId, ApiResultMessages.FailureMessage);
                Console.WriteLine(ApiResultMessages.FailureMessage);
                return false;
            }

            await this.batchClient.JobOperations.TerminateJobAsync(jobId, ApiResultMessages.SuccessMessage);

            return allTasksSuccessful;
        }

        /// <summary>
        /// Creates a Job that holds all tasks for that specific pool
        /// </summary>
        /// <param name="jobId">The id for the new job creation</param>
        /// <param name="poolId">The pool id for this job</param>
        /// <returns><c>true</c> if the job was completed</returns>
        public async Task<string> CreateJobAsync(string jobId, string poolId)
        {
            Console.WriteLine("Creating job [{0}]...", jobId);

            try
            {
                var job = this.batchClient.JobOperations.CreateJob();
                job.Id = jobId;
                job.PoolInformation = new PoolInformation { PoolId = poolId };

                await job.CommitAsync();
                return string.Empty;
            }
            catch (BatchException ex)
            {
                return ((Microsoft.Azure.Batch.Protocol.Models.BatchErrorException)ex.InnerException).Response.ReasonPhrase;
            }
        }

        /// <summary>
        /// Deleted a specific job
        /// </summary>
        /// <param name="jobId">The job id to be deleted</param>
        /// <returns>Return a task that can be awaited</returns>
        public async Task DeleteJobAsync(string jobId)
        {
            await this.batchClient.JobOperations.DeleteJobAsync(jobId);
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
