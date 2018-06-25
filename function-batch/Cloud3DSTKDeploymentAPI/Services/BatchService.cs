// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Cloud3DSTKDeploymentAPI.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloud3DSTKDeploymentAPI.Models;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Auth;
    using Microsoft.Azure.Batch.Common;
    using Microsoft.Extensions.Configuration;
    
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

            BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(batchAccountUrl, batchAccountName, batchAccountKey);

            this.batchClient = BatchClient.Open(cred);
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
        /// Creates a TURN server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be created</param>
        /// <param name="dedicatedNodes">The number of dedicated nodes inside the pool</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<bool> CreateTurnPool(string poolId, int dedicatedNodes)
        {
            try
            {
                Console.WriteLine("Creating Linux pool [{0}]...", poolId);

                // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
                // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
                var pool = this.batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: dedicatedNodes,
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
                    CommandLine = "bin/bash -c \"sudo apt-get update && sudo apt-get -y install docker.io && sudo docker run -d -p 3478:3478 -p 3478:3478/udp --restart=always zolochevska/turn-server username password realm\"",
                    UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.Admin)),
                    WaitForSuccess = true
                };

                pool.NetworkConfiguration = this.GetNetworkConfigurationForTURN();

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                // Swallow the specific error code PoolExists since that is expected if the pool already exists
                if (be.RequestInformation?.BatchError != null && be.RequestInformation.BatchError.Code == BatchErrorCodeStrings.PoolExists)
                {
                    Console.WriteLine("The pool {0} already existed when we tried to create it", poolId);
                    return false;
                }
                else
                {
                    throw; // Any other exception is unexpected
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a rendering server pool
        /// </summary>
        /// <param name="poolId">The pool ID to be created</param>
        /// <param name="dedicatedNodes">The number of dedicated nodes inside the pool</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<bool> CreateRenderingPool(string poolId, int dedicatedNodes)
        {
            try
            {
                Console.WriteLine("Creating pool [{0}]...", poolId);

                // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
                // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
                var pool = this.batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: dedicatedNodes,
                    virtualMachineSize: "Standard_NV6",  // NV-series, 6 CPU, 1 GPU, 56 GB RAM 
                    virtualMachineConfiguration: new VirtualMachineConfiguration(
                        new ImageReference(
                            offer: "WindowsServer",
                            publisher: "MicrosoftWindowsServer",
                            sku: "2016-DataCenter",
                            version: "latest"),
                        nodeAgentSkuId: "batch.node.windows amd64"));

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
                // Swallow the specific error code PoolExists since that is expected if the pool already exists
                if (be.RequestInformation?.BatchError != null && be.RequestInformation.BatchError.Code == BatchErrorCodeStrings.PoolExists)
                {
                    Console.WriteLine("The pool {0} already existed when we tried to create it", poolId);
                    return false;
                }
                else
                {
                    throw; // Any other exception is unexpected
                }
            }

            return true;
        }

        /// <summary>
        /// Creates the rendering tasks for each node inside the pool
        /// The task runs a PowerShell script to update the signaling and TURN information inside each node
        /// </summary>
        /// <param name="turnServersPool">The TURN server pool id</param>
        /// <param name="jobId">The job id for the tasks</param>
        /// <param name="signalingServerURL">The URI for the signaling server</param>
        /// <param name="signalingServerPort">The port for the signaling server</param>
        /// <param name="serverCapacity">The max number of concurrent users per rendering node</param>
        /// <returns>Returns a boolean if the creation was successful</returns>
        public async Task<bool> AddRenderingTasksAsync(string turnServersPool, string jobId, string signalingServerURL, int signalingServerPort, int serverCapacity)
        {
            var turnPool = this.batchClient.PoolOperations.GetPool(turnServersPool);
            if (turnPool == null)
            {
                return false;
            }

            var turnNodes = turnPool.ListComputeNodes();
            var topNode = turnNodes.FirstOrDefault();
            var rdp = topNode.GetRemoteLoginSettings();

            // Create a collection to hold the tasks that we'll be adding to the job
            List<CloudTask> tasks = new List<CloudTask>();

            var taskId = "StartRendering";
            var startRenderingCommand = string.Format(
                    "cmd /c powershell -command \"start-process powershell -verb runAs -ArgumentList '-ExecutionPolicy Unrestricted -file %AZ_BATCH_APP_PACKAGE_server-deploy-script#1.0%\\server_deploy.ps1 {1} {2} {3} {4} {5} {6} {7} {0} '\"",
                    this.serverPath,
                    string.Format("turn:{0}:3478", rdp.IPAddress),
                    "username",
                    "password",
                    signalingServerURL,
                    signalingServerPort,
                    5000,
                    serverCapacity);

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

            // All tasks have reached the "Completed" state, however, this does not guarantee all tasks completed successfully.
            // Here we further check each task's ExecutionInfo property to ensure that it did not encounter a scheduling error
            // or return a non-zero exit code.

            // Update the detail level to populate only the task id and executionInfo properties.
            // We refresh the tasks below, and need only this information for each task.
            detail.SelectClause = "id, executionInfo";

            foreach (CloudTask task in tasks)
            {
                // Populate the task's properties with the latest info from the Batch service
                await task.RefreshAsync(detail);

                if (task.ExecutionInformation.Result == TaskExecutionResult.Failure)
                {
                    // A task with failure information set indicates there was a problem with the task. It is important to note that
                    // the task's state can be "Completed," yet still have encountered a failure.
                    allTasksSuccessful = false;

                    Console.WriteLine("WARNING: Task [{0}] encountered a failure: {1}", task.Id, task.ExecutionInformation.FailureInformation.Message);
                    if (task.ExecutionInformation.ExitCode != 0)
                    {
                        // A non-zero exit code may indicate that the application executed by the task encountered an error
                        // during execution.
                        Console.WriteLine("WARNING: Task [{0}] returned a non-zero exit code - this may indicate task execution or completion failure.", task.Id);
                    }
                }
            }

            if (allTasksSuccessful)
            {
                Console.WriteLine("Success! All tasks completed successfully within the specified timeout period.");
            }

            return allTasksSuccessful;
        }

        /// <summary>
        /// Creates a Job that holds all tasks for that specific pool
        /// </summary>
        /// <param name="jobId">The id for the new job creation</param>
        /// <param name="poolId">The pool id for this job</param>
        /// <returns><c>true</c> if the job was completed</returns>
        public async Task CreateJobAsync(string jobId, string poolId)
        {
            Console.WriteLine("Creating job [{0}]...", jobId);

            CloudJob job = this.batchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation { PoolId = poolId };

            await job.CommitAsync();
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
        /// Deleted a specific pool
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
        /// <returns>Returns the network configuration</returns>
        private NetworkConfiguration GetNetworkConfigurationForTURN()
        {
            return new NetworkConfiguration
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
        }
    }
}
