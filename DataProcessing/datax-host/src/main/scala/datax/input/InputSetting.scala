package datax.input

import datax.config.{SettingDictionary}

trait InputConf {
  val connectionString: String
  val flushExistingCheckpoints: Option[Boolean]
  val repartition: Option[Int]
}

trait InputSetting[T<:InputConf] {
  def getInputConf(dict: SettingDictionary): T
}

