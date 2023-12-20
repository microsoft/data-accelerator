// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.test.sink

import datax.client.redis.{RedisBase, RedisServerConf}
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

class RedisTests extends AnyFlatSpec with Matchers{

  "RedisBase" should "parse connection string correctly" in {
    val connectionString = "asdfasdf.asdfasd.com:6380,password=insertpasswordhere=,ssl=True,abortConnect=False"

    RedisBase.parseConnectionString(connectionString) shouldBe RedisServerConf(
      name =  "asdfasdf.asdfasd.com",
      host = "asdfasdf.asdfasd.com",
      port = 6380,
      key = "insertpasswordhere=",
      timeout = 3000,
      isCluster = true,
      useSsl = true
    )
  }
}
