// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility.test

import java.sql.Timestamp

import datax.utility.ConcurrentDateFormat
import org.scalatest.{FlatSpec, Matchers}

class ConcurrentDateFormatTests extends FlatSpec with Matchers{
  "stringToTimestamp" should "parse strings to timestamps correctly" in {
    ConcurrentDateFormat.stringToTimestamp("08/10/2018 22:55:3") shouldBe Timestamp.valueOf("2018-08-10 22:55:03")
  }
}
