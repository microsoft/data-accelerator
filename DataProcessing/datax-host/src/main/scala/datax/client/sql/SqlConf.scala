// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.sql

case class SqlConf(name: String,
                   connectionString: String,
                   url: String,
                   encrypt: String,
                   trustServerCertificate: String,
                   hostNameInCertificate: String,
                   databaseName:String,
                   table: String,
                   writeMode: String,
                   userName:String,
                   password:String,
                   filter: String,
                   connectionTimeout: String,
                   queryTimeout: String,
                   useBulkCopy : Boolean,
                   useBulkCopyTableLock: String,
                   useBulkCopyInternalTransaction: String,
                   bulkCopyTimeout:String,
                   bulkCopyBatchSize:String
                   )
