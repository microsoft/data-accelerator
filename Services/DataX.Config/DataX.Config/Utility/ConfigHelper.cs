// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DataX.Config.Utility
{
    public static class ConfigHelper
    {
        /// <summary>
        /// Parses the account name from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>account name</returns>
        public static string ParseBlobAccountName(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<=AccountName=)(.*)(?=;AccountKey)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have AccountName";
            }

            return matched;
        }

        /// <summary>
        /// Parses the account key from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>account key</returns>
        public static string ParseBlobAccountKey(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<=AccountKey=)(.*)(?=;EndpointSuffix)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have AccountKey";
            }

            return matched;
        }
    }
}
