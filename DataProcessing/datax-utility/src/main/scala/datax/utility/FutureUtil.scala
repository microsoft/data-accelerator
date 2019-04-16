// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import scala.concurrent.{ExecutionContext, Future, Promise}

object FutureUtil {
  def failFast[T](futures: Seq[Future[T]])(implicit ec: ExecutionContext): Future[Seq[T]] = {
    val promise = Promise[Seq[T]]
    futures.foreach{f => f.onFailure{case ex => promise.failure(ex)}}
    val res = Future.sequence(futures)
    promise.completeWith(res).future
  }
}
