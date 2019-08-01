// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import java.lang.reflect.ParameterizedType
import java.net.URI

import datax.classloader.ClassLoaderHost
import datax.constants.ProductConstant
import datax.exception.EngineException
import datax.fs.HadoopClient
import org.apache.log4j.LogManager
import org.apache.spark.SparkFiles
import org.apache.spark.sql.api.java._
import org.apache.spark.sql.catalyst.ScalaReflection
import org.apache.spark.sql.expressions.{MutableAggregationBuffer, UserDefinedAggregateFunction}
import org.apache.spark.sql.types.{DataType, StructType}
import org.apache.spark.sql.{Row, SparkSession, UDFRegistration}

import scala.collection.mutable.HashMap

object SparkJarLoader {
  val currentJars = new HashMap[String, Long]

  def getJavaUDFReturnDataType(t: Class[_]): DataType = {
    val mirror = scala.reflect.runtime.universe.runtimeMirror(ClassLoaderHost.derivedClassLoader)
    val ts = mirror.classSymbol(t).typeSignature
    val udfInterface = ts.baseClasses.filter(c=>c.fullName.startsWith("org.apache.spark.sql.api.java.UDF"))(0)
    ScalaReflection.schemaFor(ts.baseType(udfInterface).typeArgs.last).dataType
  }

  def addJarOnDriver(spark: SparkSession, jarPath: String, timestamp: Long = 0, resolveStorageKey:Boolean=true) : String = {
    var fileUrl = ""
    val logger = LogManager.getLogger("AddJar")
    val localName = new URI(jarPath).getPath.split("/").last
    val currentTimeStamp = currentJars.get(jarPath)
      .orElse(currentJars.get(localName))
      .getOrElse(-1L)
    if (currentTimeStamp < timestamp) {
      logger.warn("Fetching " + jarPath + " with timestamp " + timestamp)
      // Fetch file with useCache mode, close cache for local mode..
      // resolveStorageKey controls whether to retrieve the actual jarPath from
      // keyvault for the case where jarPath is keyvault url
      HadoopClient.fetchFile(jarPath,
        new java.io.File(SparkFiles.getRootDirectory()),
        localName, resolveStorageKey)

      // Add it to our class loader
      val url = new java.io.File(SparkFiles.getRootDirectory(), localName).toURI.toURL 
	  fileUrl = url.getPath()
      if (!ClassLoaderHost.urlClassLoader.getURLs().contains(url)) {
        logger.info("Adding " + url + " to class loader")
        ClassLoaderHost.urlClassLoader.addURL(url)
      }
    }
	fileUrl
  }

  def addJar(spark: SparkSession, jarPath: String) = {
    val jarFileUrl = addJarOnDriver(spark, jarPath)
	val logger = LogManager.getLogger("AddJar to executer")
    logger.info("jarFileUrl is " + jarFileUrl)
	// Add jar file to executers from the local path in driver node
    spark.sparkContext.addJar(jarFileUrl)
  }

  def loadUdf(spark: SparkSession, udfName: String, jarPath: String, mainClass: String, method: String) = {
    addJar(spark, jarPath)
    registerJavaUDF(spark.udf, udfName, mainClass, null)
  }

  /**
    * Register a Java UDF class using reflection
    *
    * @param name   udf name
    * @param className   fully qualified class name of udf
    * @param returnDataType  return type of udf. If it is null, spark would try to infer
    *                        via reflection.
    */
  def registerJavaUDF(udfReg: UDFRegistration, name: String, className: String, returnDataType: DataType): Unit = {
    try {
      val clazz = ClassLoaderHost.classForName(className)
      val udfInterfaces = clazz.getGenericInterfaces
        .filter(_.isInstanceOf[ParameterizedType])
        .map(_.asInstanceOf[ParameterizedType])
        .filter(e => e.getRawType.isInstanceOf[Class[_]] && e.getRawType.asInstanceOf[Class[_]].getCanonicalName.startsWith("org.apache.spark.sql.api.java.UDF"))
      if (udfInterfaces.length == 0) {
        throw new EngineException(s"UDF class $className doesn't implement any UDF interface")
      } else if (udfInterfaces.length > 1) {
        throw new EngineException(s"It is invalid to implement multiple UDF interfaces, UDF class $className")
      } else {
        try {
          val udf = clazz.newInstance()
          //val udfReturnType = udfInterfaces(0).getActualTypeArguments.last
          val returnType = if(returnDataType==null) getJavaUDFReturnDataType(clazz) else returnDataType

          udfInterfaces(0).getActualTypeArguments.length match {
            case 1 => udfReg.register(name, udf.asInstanceOf[UDF0[_]], returnType)
            case 2 => udfReg.register(name, udf.asInstanceOf[UDF1[_, _]], returnType)
            case 3 => udfReg.register(name, udf.asInstanceOf[UDF2[_, _, _]], returnType)
            case 4 => udfReg.register(name, udf.asInstanceOf[UDF3[_, _, _, _]], returnType)
            case 5 => udfReg.register(name, udf.asInstanceOf[UDF4[_, _, _, _, _]], returnType)
            case 6 => udfReg.register(name, udf.asInstanceOf[UDF5[_, _, _, _, _, _]], returnType)
            case 7 => udfReg.register(name, udf.asInstanceOf[UDF6[_, _, _, _, _, _, _]], returnType)
            case 8 => udfReg.register(name, udf.asInstanceOf[UDF7[_, _, _, _, _, _, _, _]], returnType)
            case 9 => udfReg.register(name, udf.asInstanceOf[UDF8[_, _, _, _, _, _, _, _, _]], returnType)
            case 10 => udfReg.register(name, udf.asInstanceOf[UDF9[_, _, _, _, _, _, _, _, _, _]], returnType)
            case 11 => udfReg.register(name, udf.asInstanceOf[UDF10[_, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 12 => udfReg.register(name, udf.asInstanceOf[UDF11[_, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 13 => udfReg.register(name, udf.asInstanceOf[UDF12[_, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 14 => udfReg.register(name, udf.asInstanceOf[UDF13[_, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 15 => udfReg.register(name, udf.asInstanceOf[UDF14[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 16 => udfReg.register(name, udf.asInstanceOf[UDF15[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 17 => udfReg.register(name, udf.asInstanceOf[UDF16[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 18 => udfReg.register(name, udf.asInstanceOf[UDF17[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 19 => udfReg.register(name, udf.asInstanceOf[UDF18[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 20 => udfReg.register(name, udf.asInstanceOf[UDF19[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 21 => udfReg.register(name, udf.asInstanceOf[UDF20[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 22 => udfReg.register(name, udf.asInstanceOf[UDF21[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case 23 => udfReg.register(name, udf.asInstanceOf[UDF22[_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _]], returnType)
            case n =>
              throw new EngineException(s"UDF class with $n type arguments is not supported.")
          }
        } catch {
          case e @ (_: InstantiationException | _: IllegalArgumentException) =>
            throw new EngineException(s"Can not instantiate class $className, please make sure it has public non argument constructor")
        }
      }
    } catch {
      case e: ClassNotFoundException => throw new EngineException(s"Can not load class $className, please make sure it is on the classpath")
    }
  }

  /**
    * Register a Java UDAF class using reflection, for use from pyspark
    *
    * @param name     UDAF name
    * @param className    fully qualified class name of UDAF
    */
  def registerJavaUDAF(udfReg: UDFRegistration, name: String, className: String): Unit = {
    try {
      val clazz = ClassLoaderHost.classForName(className)
      if (!classOf[UserDefinedAggregateFunction].isAssignableFrom(clazz)) {
        throw new EngineException(s"class $className doesn't implement interface UserDefinedAggregateFunction")
      }
      val udaf = clazz.newInstance().asInstanceOf[UserDefinedAggregateFunction]
      udfReg.register(name, udaf)
    } catch {
      case e: ClassNotFoundException => throw new EngineException(s"Can not load class ${className}, please make sure it is on the classpath")
      case e @ (_: InstantiationException | _: IllegalArgumentException) =>
        throw new EngineException(s"Can not instantiate class ${className}, please make sure it has public non argument constructor")
    }
  }

  case class CaseUDAF(inputType: StructType, bufferType: StructType, returnType: DataType) extends UserDefinedAggregateFunction{
    override def inputSchema: StructType = inputType

    override def bufferSchema: StructType = bufferType

    override def dataType: DataType = returnType

    override def deterministic: Boolean = true

    override def initialize(buffer: MutableAggregationBuffer): Unit = ???

    override def update(buffer: MutableAggregationBuffer, input: Row): Unit = ???

    override def merge(buffer1: MutableAggregationBuffer, buffer2: Row): Unit = ???

    override def evaluate(buffer: Row): Any = ???
  }

  /**
    * Register a UDAF class derived from api using reflection
    *
    * @param name     UDAF name
    * @param className    fully qualified class name of UDAF
    */
  def registerApiUDAF(spark: SparkSession, name: String, className: String): Unit = {
    try {
      val clazz = ClassLoaderHost.classForName(className)
      val udfInterfaces = clazz.getGenericInterfaces
        .filter(_.isInstanceOf[ParameterizedType])
        .map(_.asInstanceOf[ParameterizedType])
        .filter(e => e.getRawType.isInstanceOf[Class[_]] && e.getRawType.asInstanceOf[Class[_]].getCanonicalName.startsWith(ProductConstant.ProductRoot + ".api.udf.UDAF"))

      if (udfInterfaces.length == 0) {
        throw new EngineException(s"UDF class $className doesn't implement any ${ProductConstant.ProductRoot}.api.udf.UDF interface")
      } else if (udfInterfaces.length > 1) {
        throw new EngineException(s"It is invalid to implement multiple UDF interfaces, UDF class $className")
      } else {
        try {
          val udf = clazz.newInstance()
          val typeArguments = udfInterfaces(0).getActualTypeArguments
          val (inputTypes, bufferAndOutputTypes) = typeArguments.splitAt(typeArguments.length-2)

          val returnType = ClassLoaderHost.javaTypeToDataType(bufferAndOutputTypes(1))
          val bufferType = ClassLoaderHost.javaTypeToDataType(bufferAndOutputTypes(0))

          //TODO: complete the implementation

        } catch {
          case e @ (_: InstantiationException | _: IllegalArgumentException) =>
            throw new EngineException(s"Can not instantiate class $className, please make sure it has public non argument constructor")
        }
      }
      val udaf = clazz.newInstance().asInstanceOf[UserDefinedAggregateFunction]
      spark.udf.register(name, udaf)
    } catch {
      case e: ClassNotFoundException => throw new EngineException(s"Can not load class ${className}, please make sure it is on the classpath")
      case e @ (_: InstantiationException | _: IllegalArgumentException) =>
        throw new EngineException(s"Can not instantiate class ${className}, please make sure it has public non argument constructor")
    }
  }
}
