// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.cosmosdb

/***
  * Represents the connection info for sinker to access cosmos DB
  * @param name name of this cosmosdb connection info mainly for logging purpose
  * @param connectionString connection string to the cosmosdb
  * @param database name of the cosmos database
  * @param collection name of the collection in cosmos db
  */
case class CosmosDBConf(name: String, connectionString: String, database: String, collection: String)

object CosmosDBBase {

}
