// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.classloader.ClassLoaderHost
import datax.config.{SettingDictionary, SettingNamespace}
import datax.exception.EngineException
import datax.extension.DynamicUDF._
import org.apache.spark.sql.SparkSession
import org.apache.spark.sql.catalyst.ScalaReflection
import org.apache.spark.sql.catalyst.expressions.{Expression, ScalaUDF}
import org.apache.spark.sql.types.DataType

object ExtendedUDFHandler {
  val NamespacePrefix = SettingNamespace.JobProcessPrefix + "udf."

  def getUdfClasses(dict: SettingDictionary) = {
    dict.getSubDictionary(NamespacePrefix)
  }

  def initialize(spark: SparkSession, dict: SettingDictionary) = {
    getUdfClasses(dict).getDictMap().par.map{ case(k, v)=>{
      k-> registerUdf(k, v, spark, dict)
    }}
  }

  val ClassNamePrefix = classOf[Generator0[_]].getCanonicalName.dropRight(1)

  val mirror = scala.reflect.runtime.universe.runtimeMirror(ClassLoaderHost.derivedClassLoader)

  def getUdfBaseClassNames(className: String) = {
    val clazz = ClassLoaderHost.classForName(className)
    val ts = mirror.classSymbol(clazz).typeSignature
    ts.baseClasses.map(s=>s.fullName)
  }

  def registerUdf(name: String, className: String, spark: SparkSession, dict: SettingDictionary) = {
    val clazz = ClassLoaderHost.classForName(className)
    val ts = mirror.classSymbol(clazz).typeSignature
    val udfInterfaces = ts.baseClasses.filter(c=>c.fullName.startsWith(ClassNamePrefix))

    if (udfInterfaces.length == 0) {
      throw new EngineException(s"UDF class $className doesn't implement any UDF interface")
    } else if (udfInterfaces.length > 1) {
      throw new EngineException(s"It is invalid to implement multiple UDF interfaces, UDF class $className")
    } else {
      val udfInterface = udfInterfaces(0)
      val typeArgs = ts.baseType(udfInterface).typeArgs
      val returnType = ScalaReflection.schemaFor(typeArgs.last).dataType
      val udf = clazz.newInstance()
      val argumentCount = typeArgs.length - 1
	  val inputsNullSafe = typeArgs.take(argumentCount).map(t=>{
        false
      })
      val wrap = generateFunctionRef(udf, argumentCount, spark, dict)
      registerFunction(spark, name, wrap.func, returnType, argumentCount, inputsNullSafe)
      wrap.onInterval
    }
  }

  case class UdfWrap(func: AnyRef, onInterval: IntervalUpdateHandler)

  def generateFunctionRef(udf: Any, argumentCount: Int, spark: SparkSession, dict: SettingDictionary):UdfWrap = {
    argumentCount match {
      case 0=> initializeUdf0(udf, spark, dict)
      case 1=> initializeUdf1(udf, spark, dict)
      case 2=> initializeUdf2(udf, spark, dict)
      case 3=> initializeUdf3(udf, spark, dict)
      case _=> throw new EngineException(s"UDF with $argumentCount arguments is not supported yet.")
    }
  }

  def initializeUdf0(udf: Any, spark: SparkSession, dict: SettingDictionary) = {
    val obj = udf.asInstanceOf[Generator0[Any]].initialize(spark, dict)
    UdfWrap(obj.func, obj.onInterval)
  }

  def initializeUdf1(udf: Any, spark: SparkSession, dict: SettingDictionary) = {
    val obj = udf.asInstanceOf[Generator1[Any, Any]].initialize(spark, dict)
    UdfWrap(obj.func.apply(_:Any), obj.onInterval)
  }

  def initializeUdf2(udf: Any, spark: SparkSession, dict: SettingDictionary) = {
    val obj = udf.asInstanceOf[Generator2[Any, Any, Any]].initialize(spark, dict)
    UdfWrap(obj.func.apply(_:Any, _:Any), obj.onInterval)
  }

  def initializeUdf3(udf: Any, spark: SparkSession, dict: SettingDictionary) = {
    val obj = udf.asInstanceOf[Generator3[Any, Any, Any, Any]].initialize(spark, dict)
    UdfWrap(obj.func.apply(_:Any, _:Any, _:Any), obj.onInterval)
  }

  def registerFunction(spark:SparkSession, name: String, func: AnyRef, returnType: DataType, argumentCount: Int, inputsNullSafe: Seq[Boolean]) = {
    def builder(e: Seq[Expression]) = if (e.length == argumentCount) {
      ScalaUDF(func, returnType, e, inputsNullSafe=inputsNullSafe, udfName = Some(name))
    } else {
      throw new EngineException(s"Invalid number of arguments for function $name. Expected: $argumentCount; Found: ${e.length}")
    }

    spark.sessionState.functionRegistry.createOrReplaceTempFunction(name, builder)
  }
}
