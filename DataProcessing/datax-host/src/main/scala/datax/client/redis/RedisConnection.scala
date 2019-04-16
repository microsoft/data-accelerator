// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.redis

import io.lettuce.core.api.sync.{RedisSortedSetCommands, RedisStringCommands}

trait RedisConnection{
  def getStringCommands: RedisStringCommands[String, String]
  def getSortedSetCommands: RedisSortedSetCommands[String, String]
  def reconnect(): Unit
}

