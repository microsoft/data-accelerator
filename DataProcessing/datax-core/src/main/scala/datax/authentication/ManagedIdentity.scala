// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.authentication

import org.json4s.DefaultFormats
import org.json4s.JsonAST.JNothing
import org.json4s.jackson.JsonMethods.parse

object ManagedIdentity {
  private def localMsiEndpoint="http://localhost:40381/managed/identity/oauth2/token"

  // Get the access token for the passed in MSI resource by calling into the local endpoint
  def getAccessToken(resourceId:String): String = {

    implicit val formats: DefaultFormats.type = DefaultFormats

    val endpointId = s"$localMsiEndpoint?resource=$resourceId&api-version=2018-11-01"
    val token = HttpGetter.httpGet(endpointId, Option(Map("Metadata"->"true")))
    val tokenJson = parse(token)

    var tokenstr =""

    // Retrieve the access_token value from the token object json
    if (tokenJson \ "access_token" != JNothing) {
      tokenstr = (tokenJson \ "access_token").extract[String].trim
    }

    tokenstr
  }

}
