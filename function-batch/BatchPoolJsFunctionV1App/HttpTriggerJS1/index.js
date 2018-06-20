module.exports = function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

   // Initializing Azure Batch variables
    var batch = require('azure-batch');
    var accountName = process.env.BATCH_ACCOUNT_NAME;
    var accountKey = process.env.BATCH_ACCOUNT_KEY;
    var accountUrl = process.env.BATCH_ACCOUNT_URL;

    // Create Batch credentials object using account name and account key
    var credentials = new batch.SharedKeyCredentials(accountName,accountKey);

    // Create Batch service client
    var batch_client = new batch.ServiceClient(credentials,accountUrl);
 
    // Creating Image reference configuration for Ubuntu Linux VM
    var imgRef = {
        publisher:"MicrosoftWindowsServer",
        offer:"WindowsServer",
        sku:"2016-DataCenter",
        version:"latest"
    }

    // Creating the VM configuration object with the SKUID
    var vmconfig = {
        imageReference:imgRef,
        nodeAgentSKUId:"batch.node.windows amd64"
    }

    // Setting the VM size to Standard F4
    var vmSize = "Standard_NV6"

    //Setting number of VMs in the pool to 4
    var numVMs = 1

    // Create a unique Azure Batch pool ID
    var poolid = process.env.POOL_ID;
    var poolConfig = {
        id:poolid, 
        displayName:poolid,
        vmSize:vmSize,
        virtualMachineConfiguration:vmconfig,
        targetDedicatedComputeNodes:numVMs,
        enableAutoScale:false 
    };

    // Creating the Pool for the specific customer
    var pool = batch_client.pool.add(poolConfig,function(error,result){
        if(error!=null){console.log(error.response)};
    });
    
    context.done();
};