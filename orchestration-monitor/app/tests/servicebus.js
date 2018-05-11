'use strict'

const assert = require('assert')
const servicebus = require('../../app/services/servicebus')

describe('servicebus', () => {
  describe('#sendServiceBusRequestBatch', () => {
    it('should return an empty array on an empty batch of messages', async () => {
      const batch = []
      const response = await servicebus.sendServiceBusRequestBatch(batch)

      assert(response && response.length === 0)
    })
  })
})
