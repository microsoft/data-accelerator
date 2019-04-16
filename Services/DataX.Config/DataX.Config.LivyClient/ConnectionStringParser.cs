// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Exception;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DataX.Config.LivyClient
{
    public static class ConnectionStringParser
    {
        private static readonly Regex _LivyConnectionStringFormat = new Regex("^endpoint=([^;]*);username=([^;]*);password=(.*)$");
        public static LivyClientConnectionInfo Parse(string connectionString)
        {
            if (connectionString == null)
            {
                throw new GeneralException($"connection string for livy client cannot be null");
            }

            var match = _LivyConnectionStringFormat.Match(connectionString);
            if (match == null || !match.Success)
            {
                throw new GeneralException($"cannot parse connection string to access livy service");
            }

            return new LivyClientConnectionInfo()
            {
                Endpoint = match.Groups[1].Value,
                UserName = match.Groups[2].Value,
                Password = match.Groups[3].Value
            };
        }
    }
}
