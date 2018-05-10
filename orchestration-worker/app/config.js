'use strict'

module.exports = {
  'serviceBus': {
    'connectionsString': process.env.AZURE_SERVICEBUS_CONNECTION_STRING,
    'topic': process.env.AZURE_SEVICEBUS_TOPIC || '3dtoolkit-infrastructure-topic'
  },
  'cosmos': {
    'host': process.env.COSMOS_HOST,
    'masterKey': process.env.COSMOS_MASTER_KEY
  },
  'subscription': {
    'id': process.env.AZURE_SUBSCRIPTIONID,
    'cliendId': process.env.AZURE_CLIEND_ID,
    'secret': process.env.AZURE_SECRET
  }
}
