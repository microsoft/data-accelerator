// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.UnifiedConfig
import datax.processor.{StreamingProcessor}
import org.apache.spark.rdd.RDD
import org.apache.spark.streaming.{StreamingContext, Time}

// Abstract factory class for creating DStream and invoking its processor.
// All DStream generators (based in input) derive from this class
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
