package datax.input

import datax.config.UnifiedConfig
import datax.processor.{StreamingProcessor}
import org.apache.spark.rdd.RDD
import org.apache.spark.streaming.{StreamingContext, Time}


abstract class StreamingFactory[T] {

  def getStream(streamingContext: StreamingContext,
                inputConf:InputConf,
                foreachRDDHandler: (RDD[T], Time)=>Unit
               )

  @volatile private var instance: StreamingProcessor[T] = null
  def getOrCreateProcessor(config: UnifiedConfig,
                           generator: UnifiedConfig =>StreamingProcessor[T]) = {
    if (instance == null) {
      synchronized {
        if (instance == null) {
          instance = generator(config)
        }
      }
    }

    instance
  }

}
