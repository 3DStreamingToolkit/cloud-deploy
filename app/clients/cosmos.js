'use strict'

import DocumentClient from 'documentdb'
import config from '../config'

const host = config.cosmos.host
const masterKey = config.cosmos.masterKey
const client = new DocumentClient(host, {masterKey: masterKey})

/**
 * @param {query: string, params: Array<string|map>}
 * @returns {Promise}
 */
function executeQuery (query) {
  return new Promise((resolve, reject) => {
    if (!client) return reject(new Error('Error connecting to Cosmos.'))
    resolve({})
  })
}
