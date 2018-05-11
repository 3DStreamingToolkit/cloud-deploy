module.exports = {
  'serviceBus': {
    'connectionsString': process.env.AZURE_SERVICEBUS_CONNECTION_STRING,
    'topic': process.env.AZURE_SEVICEBUS_TOPIC || '3dtoolkit-infrastructure-topic',
    'subscription': process.env.AZURE_SERVICE_SUBSCRIPTION || 'infrastructure'
  },
  'cosmos': {
    'host': process.env.COSMOS_HOST_URL,
    'masterKey': process.env.COSMOS_AUTH_KEY,
    'dbname': process.env.COSMOS_DB_NAME || '3dtoolkit',
    'collectionName': process.env.COSMOS_COLLECTION_NAME || 'servers'
  },
  'subscription': {
    'id': process.env.AZURE_SUBSCRIPTION_ID,
    'cliendId': process.env.AZURE_CLIEND_ID,
    'secret': process.env.AZURE_SECRET,
    'tenantId': process.env.AZURE_TENANT_ID,
    'resourceGroup': process.env.AZURE_RESOURCE_GROUP || '3dtoolkit'
  },
  'template': {
    'url': process.env.VM_TEMPLATE_URL
  },
  'blob': {
    'name': process.env.AZURE_STORAGE_CONNECTION_STRING,
    'container': process.env.AZURE_STORAGE_CONTAINER_NAME || '3dtoolkit',
    'prefix': process.env.AZURE_STORAGE_CONTAINER_NAME_PREFIX || 'template'
  },
  'turnServer': {
    'username': process.env.TURNSERVER_USERNAME || 'username',
    'password': process.env.TURNSERVER_PASSWORD || 'password',
    'port': process.env.TURNSERVER_PORT || '443',
    'heartbeat': process.env.TURNSERVER_HEARTBEAT || '5000',
    'vnetId': process.env.TURNSERVER_VNET_ID
  }
}
