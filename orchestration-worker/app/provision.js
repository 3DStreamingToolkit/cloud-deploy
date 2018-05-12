var util = require('util')
var config = require('./config.js')
var msRestAzure = require('ms-rest-azure')
var ComputeManagementClient = require('azure-arm-compute')
var StorageManagementClient = require('azure-arm-storage')
var NetworkManagementClient = require('azure-arm-network')
var ResourceManagementClient = require('azure-arm-resource').ResourceManagementClient

// Ubuntu config
var publisher = 'Canonical'
var offer = 'UbuntuServer'
var sku = '14.04.3-LTS'
var osType = 'Linux'

var resourceClient, computeClient, storageClient, networkClient
// Sample Config
var randomIds = {}
var location = 'eastus'
var accType = 'Standard_LRS'
var resourceGroupName = '3dtoolkit-anderm'

var vmName = _generateRandomId('turnserver-3dtoolkit', randomIds)
var storageAccountName = _generateRandomId('turnserverac', randomIds)
var vnetName = _generateRandomId('turnservervnet', randomIds)
var subnetName = _generateRandomId('turnserversubnet', randomIds)
var publicIPName = _generateRandomId('turnserverip', randomIds)
var networkInterfaceName = _generateRandomId('turnservernic', randomIds)
var ipConfigName = _generateRandomId('turnserveripconf', randomIds)
var domainNameLabel = _generateRandomId('turnserverdomain', randomIds)
var osDiskName = _generateRandomId('turnserverosdisk', randomIds)

var adminUsername = 'notadmin'
var adminPassword = 'Pa$$w0rd92'
var vmName = _generateRandomId('testvm', randomIds)
var subscriptionId = config.subscription.id

function _generateRandomId (prefix, exsitIds) {
  var newNumber
  while (true) {
    newNumber = prefix + Math.floor(Math.random() * 10000)
    if (!exsitIds || !(newNumber in exsitIds)) {
      break
    }
  }
  return newNumber
}

async function createVM () {
  msRestAzure.loginWithServicePrincipalSecret(config.subscription.cliendId, config.subscription.secret, config.subscription.tenantId, function (err, credentials, subscriptions) {
    if (err) return console.log(err)

    console.log(credentials)
    console.log(subscriptions)

    resourceClient = new ResourceManagementClient(credentials, subscriptionId)
    computeClient = new ComputeManagementClient(credentials, subscriptionId)
    storageClient = new StorageManagementClient(credentials, subscriptionId)
    networkClient = new NetworkManagementClient(credentials, subscriptionId)

    // Creates the storage account to be used by the VM
    createStorageAccount(function (err, accountInfo) {
      if (err) return err
      createVnet(function (err, vnetInfo) {
        if (err) return err
        console.log('\nCreated vnet:\n' + util.inspect(vnetInfo, {
          depth: null
        }))
        getSubnetInfo(function (err, subnetInfo) {
          if (err) return err
          console.log('\nFound subnet:\n' + util.inspect(subnetInfo, {
            depth: null
          }))
          createPublicIP(function (err, publicIPInfo) {
            if (err) return err
            console.log('===>NIC info: ' + publicIPInfo.publicIPAddress)
            console.log('\nCreated public IP:\n' + util.inspect(publicIPInfo, {
              depth: null
            }))
            createNIC(subnetInfo, publicIPInfo, function (err, nicInfo) {
              if (err) return err
              console.log('\nCreated Network Interface:\n' + util.inspect(nicInfo, {
                depth: null
              }))
              findVMImage(function (err, vmImageInfo) {
                if (err) return err
                console.log('\nFound Vm Image:\n' + util.inspect(vmImageInfo, {
                  depth: null
                }))
                getNICInfo(function (err, nicResult) {
                  if (err) {
                    console.log('Could not get the created NIC: ' + networkInterfaceName + util.inspect(err, {
                      depth: null
                    }))
                    return err
                  } else {
                    console.log('Found the created NIC: \n' + util.inspect(nicResult, {
                      depth: null
                    }))
                  }
                  createVirtualMachine(nicInfo.id, vmImageInfo[0].name, function (err, vmInfo) {
                    if (err) {
                      console.log('===>VM Id: ' + vmInfo.id)
                      console.log('\nCreated Vm Image:\n' + util.inspect(err, {
                        depth: null
                      }))
                    } else {
                      return vmInfo
                    }
                  })
                })
              })
            })
          })
        })
      })
    })

    function createStorageAccount (callback) {
      console.log('\n2.Creating storage account: ' + storageAccountName)
      var createParameters = {
        location: location,
        sku: {
          name: accType
        },
        kind: 'Storage',
        tags: {
          tag1: 'val1',
          tag2: 'val2'
        }
      }
      return storageClient.storageAccounts.create(resourceGroupName, storageAccountName, createParameters, callback)
    }

    function createVnet (callback) {
      var vnetParameters = {
        location: location,
        addressSpace: {
          addressPrefixes: ['10.0.0.0/16']
        },
        dhcpOptions: {
          dnsServers: ['10.1.1.1', '10.1.2.4']
        },
        subnets: [{
          name: subnetName,
          addressPrefix: '10.0.0.0/24'
        }]
      }
      console.log('\n3.Creating vnet: ' + vnetName)
      return networkClient.virtualNetworks.createOrUpdate(resourceGroupName, vnetName, vnetParameters, callback)
    }

    function getSubnetInfo (callback) {
      console.log('\nGetting subnet info for: ' + subnetName)
      return networkClient.subnets.get(resourceGroupName, vnetName, subnetName, callback)
    }

    function createPublicIP (callback) {
      var publicIPParameters = {
        location: location,
        publicIPAllocationMethod: 'Dynamic',
        dnsSettings: {
          domainNameLabel: domainNameLabel
        }
      }
      console.log('\n4.Creating public IP: ' + publicIPName)
      return networkClient.publicIPAddresses.createOrUpdate(resourceGroupName, publicIPName, publicIPParameters, callback)
    }

    function createNIC (subnetInfo, publicIPInfo, callback) {
      var nicParameters = {
        location: location,
        ipConfigurations: [{
          name: ipConfigName,
          privateIPAllocationMethod: 'Dynamic',
          subnet: subnetInfo,
          publicIPAddress: publicIPInfo
        }]
      }
      console.log('\n5.Creating Network Interface: ' + networkInterfaceName)
      return networkClient.networkInterfaces.createOrUpdate(resourceGroupName, networkInterfaceName, nicParameters, callback)
    }

    function findVMImage (callback) {
      console.log(util.format('\nFinding a VM Image for location %s from ' +
                'publisher %s with offer %s and sku %s', location, publisher, offer, sku))
      return computeClient.virtualMachineImages.list(location, publisher, offer, sku, {
        top: 1
      }, callback)
    }

    function getNICInfo (callback) {
      return networkClient.networkInterfaces.get(resourceGroupName, networkInterfaceName, callback)
    }

    function createVirtualMachine (nicId, vmImageVersionNumber, callback) {
      var vmParameters = {
        location: location,
        osProfile: {
          computerName: vmName,
          adminUsername: adminUsername,
          adminPassword: adminPassword
        },
        hardwareProfile: {
          vmSize: 'Basic_A0'
        },
        storageProfile: {
          imageReference: {
            publisher: publisher,
            offer: offer,
            sku: sku,
            version: vmImageVersionNumber
          },
          osDisk: {
            name: osDiskName,
            caching: 'None',
            createOption: 'fromImage',
            vhd: {
              uri: 'https://' + storageAccountName + '.blob.core.windows.net/nodejscontainer/osnodejslinux.vhd'
            }
          }
        },
        networkProfile: {
          networkInterfaces: [{
            id: nicId,
            primary: true
          }]
        }
      }
      console.log('\n6.Creating Virtual Machine: ' + vmName)
      console.log('\n VM create parameters: ' + util.inspect(vmParameters, {
        depth: null
      }))
      computeClient.virtualMachines.createOrUpdate(resourceGroupName, vmName, vmParameters, callback)
    }
  })
}

module.exports = {
  createVM
}
