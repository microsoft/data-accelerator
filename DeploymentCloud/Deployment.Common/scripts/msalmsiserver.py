# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

#!/usr/bin/env python

import msal
import json

from urlparse import urlparse
from BaseHTTPServer import BaseHTTPRequestHandler, HTTPServer
import hdinsight_common.ClusterManifestParser as ClusterManifestParser

"""
This script exposes a local http endpoint which the spark jobs can call to get the MSI access token associated with the HDI cluster. 
Note that since it's a local endpoint, it's accessible only from within the cluster and not from outside.

Usage:
http://localhost:40382/managed/identity/oauth2/token?resource=<resourceid>&api-version=2018-11-01

Example:
curl -H "Metadata: true" -X GET "http://localhost:40382/managed/identity/oauth2/token?resource=https://vault.azure.net&api-version=2018-11-01"
"""

class Constants(object):
    loopback_address = '127.0.0.1'
    server_port = 40382
    token_url_path = '/managed/identity/oauth2/token'
    header_metadata = 'Metadata'
    query_resource = 'resource'
    cert_location = '/var/lib/waagent/{0}.prv'
    aad_login_endpoint = 'https://login.microsoftonline.com/{0}'

class ManagedIdentityTokenResponse(object):
    def __init__(self):
        self.access_token = None
        self.token_type = None
        self.resource = None

class ManagedIdentityHandler(BaseHTTPRequestHandler):
    def _add_to_query_dict(self, query_dict, query):
        query_dict[query.split('=')[0]] = query.split('=')[1]

    def _validate_request(self):
        msg = ''

        if self.headers[Constants.header_metadata] != 'true':
            msg += 'Metadata header is required\n'

        if self.client_address[0] != Constants.loopback_address:
            msg += 'Only request from loopback address 127.0.0.1 is allowed\n'

        url = urlparse(self.path)
        if url.path != Constants.token_url_path:
            msg += 'Unknown path {0}\n'.format(url.path)

        return msg

    def _get_cluster_manifest(self):
        return ClusterManifestParser.parse_local_manifest()

    def _get_private_key(self, filename):
        with open(filename, 'r') as cert_file:
            private_cert = cert_file.read()
        return private_cert

    def _acquire_token(self, resource):
        cluster_manifest = self._get_cluster_manifest()
        msi_settings = json.loads(cluster_manifest.settings['managedServiceIdentity'])
        # Assuming there is only 1 MSI associated with the cluster, get the first one
        msi_setting = list(msi_settings.values())[0]

        thumbprint = msi_setting['thumbprint']
        client_id = msi_setting['clientId']
        tenant_id = msi_setting['tenantId']

        authority = Constants.aad_login_endpoint.format(tenant_id)
        file_name = Constants.cert_location.format(thumbprint)
        key = self._get_private_key(file_name)

        app = msal.ConfidentialClientApplication(
            client_id,
            authority=authority,
            client_credential={"private_key": key, "thumbprint": thumbprint}
        )

        auth_result = app.acquire_token_for_client(scopes=[resource])

        res = ManagedIdentityTokenResponse()
        res.access_token = auth_result['access_token']
        res.token_type = auth_result['token_type']
        res.resource = resource

        return res

    def do_GET(self):
        try:
            msg = self._validate_request()

            if msg:
                self.send_response(400)
                self.end_headers()
                self.wfile.write(msg)
                return

            url = urlparse(self.path)
            queries = {}
            map(lambda q: self._add_to_query_dict(queries, q), url.query.split('&'))
            res = self._acquire_token(queries[Constants.query_resource])

            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps(res.__dict__))
        except Exception:
            self.send_response(500)
            self.end_headers()
            self.wfile.write("Internal server error, please see server log")

if __name__ == "__main__":
    server_address = (Constants.loopback_address, Constants.server_port)
    httpd = HTTPServer(server_address, ManagedIdentityHandler)
    print('Starting http server...')
    httpd.serve_forever()