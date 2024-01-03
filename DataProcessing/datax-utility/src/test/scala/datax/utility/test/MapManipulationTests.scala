package datax.utility.test

import datax.utility.MapManipulation.mergeMapOfNullableValues
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

class MapManipulationTests extends AnyFlatSpec with Matchers {

  "mergeMapOfNullableValues" should "merge maps of nullable values correctly" in {
    val map1 = Map("a"->Some("1"), "b"->Some("2"), "c"->Some("10"))
    val map2 = Map("a"->Some("3"), "c"->None)
    val merged = mergeMapOfNullableValues[String, String](map1,map2)
    merged shouldBe Map("a"->Some("3"), "b"->Some("2"), "c"->None)
  }

  "mergeMapOfNullableValues" should "handle null maps gracefully" in {
    val map1: Map[String, Option[String]] = null
    val map2 = Map("a" -> Some("3"), "c" -> None)
    val merged = mergeMapOfNullableValues[String, String](map1, map2)
    merged shouldBe Map("a" -> Some("3"), "c" -> None)
  }

}
