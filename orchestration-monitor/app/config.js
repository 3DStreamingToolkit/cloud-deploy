'use strict'

module.exports = {
  'serviceBus': {
    'connectionString': process.env.AZURE_SERVICEBUS_CONNECTION_STRING,
    'topic': process.env.AZURE_SEVICEBUS_TOPIC || '3dtoolkit-infrastructure-topic',
    'subscription': process.env.SERVICE_BUS_SUBSCRIPTION || 'infrastructure',
    'maxAsyncCalls': process.env.SERVICEBUS_MAX_ASYNC_CALLS || 10
  },
  'cosmos': {
    'host': process.env.HOST,
    'masterKey': process.env.COSMOS_MASTER_KEY,
    'maxAsyncCalls': process.env.COSMOS_MAX_ASYNC_CALLS || 1000
  }
}
