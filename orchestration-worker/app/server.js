'use strict'

var config = require('./config.js')
var azure = require('azure')

var serviceBusService = azure.createServiceBusService(config.serviceBus.connectionsString)

// Create topic if doesnt exits
serviceBusService.createTopicIfNotExists(config.serviceBus.topic, function (error) {
  if (!error) {
    // Topic was created or exists
    console.log('topic created or exists.')
  }
})

serviceBusService.receiveSubscriptionMessage(config.serviceBus.topic, 'HighMessages', { isPeekLock: true }, function (error, lockedMessage) {
  if (!error) {
    // Message received and locked
    console.log(lockedMessage)

    // create pool
    if (lockedMessage.body === 'create') {
      createPool(null, null, null)
    }

    if (lockedMessage.body === 'delete') {
      // delete pool
    }

    serviceBusService.deleteMessage(lockedMessage, function (deleteError) {
      if (!deleteError) {
        // Message deleted
        console.log('message has been deleted.')
      }
    })
  }
})

var createPool = function (vmCount, turnSeverUsername, turnServerPassword) {
  // createTurnServerAndWait(resourceGroup)
  // createScript(turnServerIP)
  // createVMsandForget(vmCount, resourceGroup, scriptLocation)
  // updateOrchestationDB(connectionString)
  // clearQueueMessage()
}
