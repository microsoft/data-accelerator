// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Exception;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DataX.Config.DatabricksClient
{
    public static class ConnectionStringParser
    {
        private static readonly Regex _DatabricksConnectionStringFormat = new Regex("^endpoint=([^;]*);dbtoken=(.*)$");

        public static DatabricksClientConnectionInfo Parse(string connectionString)
        {
            if (connectionString == null)
            {
                throw new GeneralException($"connection string for livy client cannot be null");
            }

            var match = _DatabricksConnectionStringFormat.Match(connectionString);
            if (match == null || !match.Success)
            {
                throw new GeneralException($"cannot parse connection string to access livy service");
            }

            return new DatabricksClientConnectionInfo()
            {
                Endpoint = match.Groups[1].Value,
                DbToken = match.Groups[2].Value
            };
        }
    }
}
