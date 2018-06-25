using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Extensions.Configuration;

namespace Cloud3DSTKDeploymentAPI.Services
{
    public class BatchService : IBatchService
    {
        public static IConfiguration Configuration { get; set; }

        // Batch account credentials
        private static string BatchAccountName;
        private static string BatchAccountKey;
        private static string BatchAccountUrl;

        // VM internal folder for streaming server
        private static string serverPath = "C:/3dstk/Server";
        private BatchClient batchClient;

        public BatchService(IConfiguration configuration)
        {
            Configuration = configuration;

            BatchAccountName = Configuration["BatchAccountName"];
            BatchAccountKey = Configuration["BatchAccountKey"];
            BatchAccountUrl = Configuration["BatchAccountUrl"];

            BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

            batchClient = BatchClient.Open(cred);
        }

        public IList<CloudPool> GetPoolsInBatch()
        {
            var poolData = batchClient.PoolOperations.ListPools();
            return poolData.ToList();
        }

        public async Task<bool> CreateLinuxPool(string poolId, int dedicatedNodes)
        {
            try
            {
                Console.WriteLine("Creating Linux pool [{0}]...", poolId);

                // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
                // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
                var pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    //targetDedicatedComputeNodes: 3, // 3 compute nodes
                    targetDedicatedComputeNodes: 1,
                    virtualMachineSize: "STANDARD_A1",  // NV-series, 6 CPU, 1 GPU, 56 GB RAM 
                                                        //virtualMachineSize: "STANDARD_A1",  // single-core, 1.75 GB memory, 225 GB disk

                    virtualMachineConfiguration: new VirtualMachineConfiguration(
                        new ImageReference(
                            offer: "UbuntuServer",
                            publisher: "Canonical",
                            sku: "14.04.5-LTS"),
                        nodeAgentSkuId: "batch.node.ubuntu 14.04")
                    );

                // Create and assign the StartTask that will be executed when compute nodes join the pool.
                // In this case, we copy the StartTask's resource files (that will be automatically downloaded
                // to the node by the StartTask) into the shared directory that all tasks will have access to.
                pool.StartTask = new StartTask
                {
                    // Since a successful execution of robocopy can return a non-zero exit code (e.g. 1 when one or
                    // more files were successfully copied) we need to manually exit with a 0 for Batch to recognize
                    // StartTask execution success.
                    CommandLine = "bin/bash -c \"sudo apt-get update && sudo apt-get -y install docker.io && sudo docker run -d -p 3478:3478 -p 3478:3478/udp --restart=always zolochevska/turn-server username password realm\"",
                    UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.Admin)),
                    WaitForSuccess = true
                };

                pool.NetworkConfiguration = GetNetworkConfigurationForTURN();

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

        public async Task<bool> CreateWindowsPool(string poolId, int dedicatedNodes)
        {
            try
            {
                Console.WriteLine("Creating pool [{0}]...", poolId);

                // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
                // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
                var pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: dedicatedNodes,
                    virtualMachineSize: "Standard_NV6",  // NV-series, 6 CPU, 1 GPU, 56 GB RAM 

                    virtualMachineConfiguration: new VirtualMachineConfiguration(
                        new ImageReference(
                            offer: "WindowsServer",
                            publisher: "MicrosoftWindowsServer",
                            sku: "2016-DataCenter",
                            version: "latest"),
                        nodeAgentSkuId: "batch.node.windows amd64")
                    );

                // Create and assign the StartTask that will be executed when compute nodes join the pool.
                // In this case, we copy the StartTask's resource files (that will be automatically downloaded
                // to the node by the StartTask) into the shared directory that all tasks will have access to.
                pool.StartTask = new StartTask
                {
                    // Install all packages and run Unit tests to ensure the node is ready for streaming
                    CommandLine = String.Format("cmd /c robocopy %AZ_BATCH_APP_PACKAGE_sample-server#1.0% {0} /E && " +
                    "cmd /c %AZ_BATCH_APP_PACKAGE_vc-redist#2015%\\vc_redist.x64.exe /install /passive /norestart && " +
                    "cmd /c %AZ_BATCH_APP_PACKAGE_NVIDIA#391.58%\\setup.exe /s && " +
                    "cmd /c %AZ_BATCH_APP_PACKAGE_native-server-tests#1%\\NativeServerTests\\NativeServer.Tests.exe --gtest_also_run_disabled_tests --gtest_filter=\"*Driver*:*Hardware*\"",
                    serverPath),
                    UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.Admin)),
                    WaitForSuccess = true,
                    MaxTaskRetryCount = 2
                };

                // Specify the application and version to install on the compute nodes
                pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                    {
                        new ApplicationPackageReference {
                            ApplicationId = "NVIDIA",
                            Version = "391.58" },
                        new ApplicationPackageReference {
                            ApplicationId = "native-server-tests",
                            Version = "1" },
                        new ApplicationPackageReference {
                            ApplicationId = "sample-server",
                            Version = "1.0" },
                         new ApplicationPackageReference {
                            ApplicationId = "vc-redist",
                            Version = "2015" },
                         new ApplicationPackageReference {
                            ApplicationId = "server-deploy-script",
                            Version = "1.0" },
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

        public async Task<bool> AddWindowsTasksAsync(string turnServersPool, string jobId, string signalingServerURL, int signalingServerPort, int serverCapacity)
        {
            var turnPool = batchClient.PoolOperations.GetPool(turnServersPool);
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
            var startRenderingCommand = String.Format("cmd /c powershell -command \"start-process powershell -verb runAs -ArgumentList '-ExecutionPolicy Unrestricted -file %AZ_BATCH_APP_PACKAGE_server-deploy-script#1.0%\\server_deploy.ps1 {1} {2} {3} {4} {5} {6} {7} {0} '\"",
                serverPath,
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
            await batchClient.JobOperations.AddTaskAsync(jobId, tasks);

            return true;
        }

        /// <summary>
        /// Monitors the specified tasks for completion and returns a value indicating whether all tasks completed successfully
        /// within the timeout period.
        /// </summary>
        /// <param name="batchClient">A <see cref="BatchClient"/>.</param>
        /// <param name="jobId">The id of the job containing the tasks that should be monitored.</param>
        /// <param name="timeout">The period of time to wait for the tasks to reach the completed state.</param>
        /// <returns><c>true</c> if all tasks in the specified job completed with an exit code of 0 within the specified timeout period, otherwise <c>false</c>.</returns>
        public async Task<bool> MonitorTasks(string jobId, TimeSpan timeout)
        {
            bool allTasksSuccessful = true;
            const string successMessage = "All tasks reached state Completed.";
            const string failureMessage = "One or more tasks failed to reach the Completed state within the timeout period.";

            // Obtain the collection of tasks currently managed by the job. Note that we use a detail level to
            // specify that only the "id" property of each task should be populated. Using a detail level for
            // all list operations helps to lower response time from the Batch service.
            ODATADetailLevel detail = new ODATADetailLevel(selectClause: "id");
            List<CloudTask> tasks = await batchClient.JobOperations.ListTasks(jobId, detail).ToListAsync();

            Console.WriteLine("Awaiting task completion, timeout in {0}...", timeout.ToString());
            
            // We use a TaskStateMonitor to monitor the state of our tasks. In this case, we will wait for all tasks to
            // reach the Completed state.
            TaskStateMonitor taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();
            try
            {
                await taskStateMonitor.WhenAll(tasks, TaskState.Completed, timeout);
            }
            catch (TimeoutException)
            {
                await batchClient.JobOperations.TerminateJobAsync(jobId, failureMessage);
                Console.WriteLine(failureMessage);
                return false;
            }

            await batchClient.JobOperations.TerminateJobAsync(jobId, successMessage);

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
                        // during execution. As not every application returns non-zero on failure by default (e.g. robocopy),
                        // your implementation of error checking may differ from this example.

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

        public async Task CreateJobAsync(string jobId, string poolId)
        {
            Console.WriteLine("Creating job [{0}]...", jobId);

            CloudJob job = batchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation { PoolId = poolId };

            await job.CommitAsync();
        }

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
                        frontendPortRangeEnd: 3578,
                        networkSecurityGroupRules: new NetworkSecurityGroupRule[]
                        {
                            new NetworkSecurityGroupRule(
                                priority: 151,
                                access: NetworkSecurityGroupRuleAccess.Allow,
                                sourceAddressPrefix: "*"),
                        }),

                    new InboundNatPool(
                        name: "UDP_5349",
                        protocol: InboundEndpointProtocol.Udp,
                        backendPort: 5349,
                        frontendPortRangeStart: 5349,
                        frontendPortRangeEnd: 5449,
                        networkSecurityGroupRules: new NetworkSecurityGroupRule[]
                        {
                            new NetworkSecurityGroupRule(
                                priority: 161,
                                access: NetworkSecurityGroupRuleAccess.Allow,
                                sourceAddressPrefix: "*"),
                        }),
                   }.ToList())
            };
        }

        public async Task DeleteJobAsync(string jobId)
        {
            await batchClient.JobOperations.DeleteJobAsync(jobId);
        }

        public async Task DeletePoolAsync(string poolId)
        {
            await batchClient.PoolOperations.DeletePoolAsync(poolId);
        }
    }
}
