// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DataX.Config.KeyVault
{
    public static class HashGenerator
    {
        /// <summary>
        /// Generates a hashcode for the input
        /// </summary>
        /// <param name="value"></param>
        /// <returns>hashcode for the input</returns>
        public static string GetHashCode(string value)
        {
            HashAlgorithm hash = SHA256.Create();
            var hashedValue = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(hashedValue).Replace("-", string.Empty).Substring(0,32);
        }
    }
}
