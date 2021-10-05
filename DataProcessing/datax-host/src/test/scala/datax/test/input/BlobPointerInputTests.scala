package datax.test.input

import datax.input.BlobPointerInput
import org.scalatest.{FlatSpec, Matchers}

class BlobPointerInputTest extends FlatSpec with Matchers{
  org.apache.log4j.BasicConfigurator.configure()

  "BlobPointerInput class" should "initialize correctly" in {
    BlobPointerInput
  }

}