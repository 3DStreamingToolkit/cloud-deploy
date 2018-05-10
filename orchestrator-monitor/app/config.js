'use strict'

export const serviceBus = {
  sbQueue: process.env.SB_QUEUE,
  sbConnStr: process.env.SB_CONN_STR
}

export const cosmos = {
  host: process.env.HOST,
  masterKey: process.env.COSMOS_MASTER_KEY
}
