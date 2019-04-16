// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;

namespace DataX.Config
{
    public static class VersionGeneration
    {
        private static string _LastGeneratedVersion = null;

        public static string Next()
        {
            var ver = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            _LastGeneratedVersion = ver;
            return ver;
        }
    }
}
