const config = require('./config.js')
const azure = require('azure')
const ServerStore = require('3dtoolkit-server-store')
const provision = require('./provision')

let serverStore = new ServerStore()
let serviceBusService = azure.createServiceBusService(config.serviceBus.connectionsString)

// Set Database Option
let options = {
  cosmosDbEndpoint: config.cosmos.host,
  cosmosDbKey: config.cosmos.masterKey,
  databaseName: config.cosmos.dbname, // defaults to '3dtoolkit'
  collectionName: config.cosmos.collectionName, // defaults to 'servers'
  collectionRUs: 1000 // defaults to 1000
}

//  Initialize Database
serverStore.init(options, (error) => {
  if (error) {
    console.error('Error initializing db')
  }
})

// Create topic if doesnt exits
serviceBusService.createTopicIfNotExists(config.serviceBus.topic, function (error) {
  if (!error) {
    // Topic was created or exists
    console.log('topic created or exists.')
  }
})

console.log('Looking for messages in ' + config.serviceBus.topic)
setInterval(function () {
  serviceBusService.receiveSubscriptionMessage(config.serviceBus.topic, config.serviceBus.subscription, {
    isPeekLock: true,
    timeoutIntervalInS: 600
  }, function (error, lockedMessage) {
    if (!error) {
      // Message received and locked
      console.log(lockedMessage)

      // SAMPLE
      // {"action":"create","count": "2"}
      // {"action":"delete","turnServerId":2,"vmIds":[1,2,3,4]}
      // {"action":"terminate"}
      var monitorRequest = JSON.parse(lockedMessage.body)

      console.log('Found action: ' + monitorRequest.action)
      if (monitorRequest.action === 'create') {
        createPool(monitorRequest.count, config.turnServer.username, config.turnServer.password)
      }

      if (monitorRequest.action === 'delete') {
        deletePool(monitorRequest.turnServerId, monitorRequest.vmIds)
      }

      if (monitorRequest.action === 'terminate') {
        terminatePools()
      }

      serviceBusService.deleteMessage(lockedMessage, function (deleteError) {
        if (!deleteError) {
        // Message deleted
          console.log('message has been deleted.')
        }
      })
    } else {
      console.error(error)
    }
  })
  process.stdout.write('.')
}, 1000)

var createPool = function (vmCount, turnSeverUsername, turnServerPassword) {
  var turnServer = createTurnServerAndWait(config.resourceGroup) // TODO: then or async
  var scriptLocation = createScript(turnServer.IP, config.template.url)
  var vmIds = createVMsandForget(vmCount, config.resourceGroup, scriptLocation)
  updateOrchestationDB(turnServer.Id, vmIds)
}

var createTurnServerAndWait = function (resourceGroup) { // TODO: remove resource group given the config is extracted in module
  console.log('createTurnServerAndWait')
  provision.createVM() // TODO: pass the right arguments
  return { Id: '1', IP: '1.1.1.1' } // return values from the turn server created
}

var createScript = function (turnServerIP, scriptTemplateLocation) {
  console.log('Instatiating VM script from template...')
  // config.turnServer.template
  console.log('Uploading script to blob...')
  return 'https://blob'
}

var createVMsandForget = function (vmCount, resourceGroup, scriptLocation) {
  var vmIds = []
  for (var i; i < vmCount; i++) {
    // TODO: create VM
  }
  return vmIds
}

var updateOrchestationDB = function (turnServer, vmIds) {
  // insert a record for each vm assigned to a turn server
  for (var vmId in vmIds) {
    var server = {
      serverId: turnServer.Id,
      azureServerId: vmId,
      turnServerId: turnServer.Id
    }

    // insert record in the DB
    serverStore.upsert(server, (error) => {
      if (error) {
        console.error('Error inserting sever into db')
        return error
      }
    })
  }
}

var deletePool = function (turnserverId, vmServersIds) {
  // TODO: delete turn server first then update the orch db
  // Delete vm servers
  for (var vmId in vmServersIds) {
    deleteAzureVM(vmId)
  }
}

var deleteAzureVM = function (vmId) {
  console.log('Deleting Azure VM' + vmId + '...')
  process.stdout.write('Deleted.')
}

var terminatePools = function () {
  console.log('Terminating All 3D Toolkit VMs...')
  process.stdout.write('Terminated.')
  // TODO: go to the orch db and remove all vms in Azure, clean the db entries as azure deletes them
}
