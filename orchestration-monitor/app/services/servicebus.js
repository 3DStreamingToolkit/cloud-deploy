'use strict'

const serviceBus = require('azure-sb')
const eachOfLimit = require('async').eachOfLimit
const config = require('../config')

const connectionString = config.serviceBus.connectionString
const topicName = config.serviceBus.topic
const subscriptionName = config.serviceBus.subscription
const maxAsyncCalls = config.serviceBus.maxAsyncCalls

const serviceBusService = serviceBus.createServiceBusService(connectionString)

/**
 * @param {Array<{Object}>} batch
 */
async function sendServiceBusRequestBatch (batch) {
  if (!serviceBusService) throw new Error('Failed to initialize service bus service.')

  if (!batch || batch.length === 0) {
    console.error('Attempted to send service bus empty request.')
    return batch
  }

  try {
    await createTopicIfNotExists()
  } catch (error) {
    console.error(`Failed to create topic ${topicName}.`)
    return error
  }

  try {
    await createSubscription()
  } catch (error) {
    console.error(`Failed to create subscription ${subscriptionName}.`)
    return error
  }

  await eachOfLimit(batch, maxAsyncCalls, sendTopicMessage, (error) => {
    if (error) {
      console.error('Failed to send message batch to service bus.')
      return error
    }
    return batch
  })
}

async function createSubscription () {
  await serviceBusService.createSubscription(topicName, subscriptionName, (error) => {
    if (error) {
      console.error(`Failed to create subscription ${subscriptionName}.`)
      return error
    }
    return subscriptionName
  })
}

async function createTopicIfNotExists () {
  await serviceBusService.createTopicIfNotExists(topicName, (error) => {
    if (error) {
      console.error(`Failed to create topic ${topicName}.`)
      return error
    }
    return topicName
  })
}

async function sendTopicMessage (message) {
  try {
    message = JSON.stringify(message)
  } catch (error) {
    return error
  }

  serviceBusService.sendTopicMessage(topicName, message, (error) => {
    if (error) {
      console.error(`Failed to send message ${message} to service bus.`)
      return error
    }
    console.log(`Sent to topic ${topicName} message: ${message}`)
    return message
  })
}

module.exports = {
  sendServiceBusRequestBatch
}
