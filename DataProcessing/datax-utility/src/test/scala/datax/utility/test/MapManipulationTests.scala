package datax.utility.test

import datax.utility.MapManipulation.mergeMapOfNullableValues
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

class MapManipulationTests extends AnyFlatSpec with Matchers {
  "mergeMapOfNullableValues" should "merge maps of nullable values correctly" in {
    val map1 = Map("a"->Some("1"), "b"->Some("2"), "c"->Some("10"))
    val map2 = Map("a"->Some("3"), "c"->Option.empty[String])
    val merged = mergeMapOfNullableValues(map1, map2)
    merged shouldBe Map("a"->Some("3"), "b"->Some("2"), "c"->None)
  }

  "mergeMapOfNullableValues" should "merge map with full null values in one side correctly" in {
    val map1 = Map("a"->Some(1.0), "b"->Some(2.0), "c"->Some(10.0))
    val map2 = Map("d"->Option.empty[Double], "e"->Option.empty[Double], "f"->Option.empty[Double])
    val merged = mergeMapOfNullableValues(map1, map2)
    merged shouldBe Map("a"->Some(1.0), "b"->Some(2.0), "c"->Some(10.0), "d"->None, "e"->None, "f"->None)
  }

  "mergeMapOfNullableValues" should "merge map with full null values in both sides correctly" in {
    val map1 = Map("a" -> Option.empty[Double], "b" -> Option.empty[Double], "c" -> Option.empty[Double])
    val map2 = Map("d" -> Option.empty[Double], "e" -> Option.empty[Double], "f" -> Option.empty[Double])
    val merged = mergeMapOfNullableValues(map1, map2)
    merged shouldBe Map("a" -> None, "b" -> None, "c" -> None, "d" -> None, "e" -> None, "f" -> None)
  }

  "mergeMapOfNullableValues" should "handle null map in one of the arguments gracefully" in {
    val map1: Map[String, Option[String]] = null
    val map2 = Map("a" -> Some("3"), "c" -> Option.empty[String])
    val merged = mergeMapOfNullableValues(map1, map2)
    merged shouldBe Map("a" -> Some("3"), "c" -> None)
  }

  "mergeMapOfNullableValues" should "handle null maps in both arguments gracefully" in {
    val map1: Map[String, Option[String]] = null
    val map2: Map[String, Option[String]] = null
    val merged = mergeMapOfNullableValues(map1, map2)
    merged shouldBe Map()
  }
}
