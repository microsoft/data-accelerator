// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.classloader

import java.net.{URL, URLClassLoader}

import org.apache.spark.SparkEnv
import org.apache.spark.sql.catalyst.ScalaReflection

import scala.collection.JavaConverters._
import scala.collection.mutable.HashMap

object ClassLoaderHost {
  /**
    * Get the ClassLoader which loaded Spark.
    */
  def getSparkClassLoader: ClassLoader = getClass.getClassLoader

  /**
    * Get the Context ClassLoader on this thread or, if not present, the ClassLoader that
    * loaded Spark.
    */
  def getContextOrSparkClassLoader: ClassLoader =
    Option(Thread.currentThread().getContextClassLoader).getOrElse(getSparkClassLoader)

  /**
    * Create a ClassLoader for use in tasks, adding any JARs specified by the user or any classes
    * created by the interpreter to the search path
    */
  private def createClassLoader(): MutableURLClassLoader = {
    // Bootstrap the list of jars with the user class path.
    val now = System.currentTimeMillis()
    userClassPath.foreach { url =>
      currentJars(url.getPath().split("/").last) = now
    }

    val currentLoader = getContextOrSparkClassLoader
    val userClassPathFirst = true

    // For each of the jars in the jarSet, add them to the class loader.
    // We assume each of the files has already been fetched.
    val urls = userClassPath.toArray ++ currentJars.keySet.map { uri =>
      new java.io.File(uri.split("/").last).toURI.toURL
    }
    if (userClassPathFirst) {
      new ChildFirstURLClassLoader(urls, currentLoader)
    } else {
      new MutableURLClassLoader(urls, currentLoader)
    }
  }

  val userClassPath: Seq[URL] = Nil
  val currentJars = new HashMap[String, Long]
  val urlClassLoader = createClassLoader()
  val derivedClassLoader = urlClassLoader

  val env = SparkEnv.get
  if(env!=null){
    // Set the classloader for serializer
    env.serializer.setDefaultClassLoader(derivedClassLoader)
    // SPARK-21928.  SerializerManager's internal instance of Kryo might get used in netty threads
    // for fetching remote cached RDD blocks, so need to make sure it uses the right classloader too.
    env.serializerManager.setDefaultClassLoader(derivedClassLoader)
  }

  /** Preferred alternative to Class.forName(className) */
  def classForName(className: String): Class[_] = {
    Class.forName(className, true, derivedClassLoader)
    // scalastyle:on classforname
  }

  def getType[T](clazz: Class[T])(implicit runtimeMirror: scala.reflect.runtime.universe.Mirror) =
    runtimeMirror.classSymbol(clazz).toType

  def javaTypeToDataType(t: java.lang.reflect.Type) = {
    val mirror = scala.reflect.runtime.universe.runtimeMirror(derivedClassLoader)
    //TODO: ParameterizedType (aka. generic type) cannot be casted to Class[_],
    //      thus getJavaUDFReturnDataType should be used instead in most of the case.
    val udfScalaType = mirror.classSymbol(t.asInstanceOf[Class[_]]).toType
    ScalaReflection.schemaFor(udfScalaType).dataType
  }
}


/**
  * A class loader which makes some protected methods in ClassLoader accessible.
  */
class ParentClassLoader(parent: ClassLoader) extends ClassLoader(parent) {

  override def findClass(name: String): Class[_] = {
    super.findClass(name)
  }

  override def loadClass(name: String): Class[_] = {
    super.loadClass(name)
  }

  override def loadClass(name: String, resolve: Boolean): Class[_] = {
    super.loadClass(name, resolve)
  }

}

/**
  * URL class loader that exposes the `addURL` and `getURLs` methods in URLClassLoader.
  */
class MutableURLClassLoader(urls: Array[URL], parent: ClassLoader)
  extends URLClassLoader(urls, parent) {

  override def addURL(url: URL): Unit = {
    super.addURL(url)
  }

  override def getURLs(): Array[URL] = {
    super.getURLs()
  }

}

/**
  * A mutable class loader that gives preference to its own URLs over the parent class loader
  * when loading classes and resources.
  */
class ChildFirstURLClassLoader(urls: Array[URL], parent: ClassLoader)
  extends MutableURLClassLoader(urls, null) {

  private val parentClassLoader = new ParentClassLoader(parent)

  override def loadClass(name: String, resolve: Boolean): Class[_] = {
    try {
      super.loadClass(name, resolve)
    } catch {
      case e: ClassNotFoundException =>
        parentClassLoader.loadClass(name, resolve)
    }
  }

  override def getResource(name: String): URL = {
    val url = super.findResource(name)
    val res = if (url != null) url else parentClassLoader.getResource(name)
    res
  }

  override def getResources(name: String): java.util.Enumeration[URL] = {
    val childUrls = super.findResources(name).asScala
    val parentUrls = parentClassLoader.getResources(name).asScala
    (childUrls ++ parentUrls).asJavaEnumeration
  }

  override def addURL(url: URL) {
    super.addURL(url)
  }

}