Data Accelerator for Spark Engine

# Project Structure

## Core
Interface and classes definition for contracts of the Data Accelerator Engine

## Host
Spark-specific app jar for Data Accelerator

## Samples
Examples for UDFs and UDAFs in Scala

## Utility
Common classes and singleton helps used across projects

# Properties

Some basic rules:
* Property names are akin to a full JSON path to locate a leaf in the JSON object
* The root namespace is **datax.job**
* Property names are all lowercase for known fields from the JSON config object, except cases from Map and Array
* Map case - e.g. the outputs is a Map of string to individual output config, in this case, put the string into the property name as part of the path
* Array case - e.g. the timeWindows is an Array of time window specs, in this case, extract the name as part of the path into property name
* When flatten Map/Array, change the plural words into singular term, e.g. change *outputs* to *output*, *timeWindows* to *timewindow*, etc.

