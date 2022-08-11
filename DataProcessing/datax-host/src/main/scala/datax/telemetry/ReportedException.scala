package datax.telemetry

final case class ReportedException(throwable: Throwable) extends Exception(throwable)