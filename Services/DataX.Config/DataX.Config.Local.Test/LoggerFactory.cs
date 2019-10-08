// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.Test.Utility.Mock;
using Microsoft.Extensions.Logging;

namespace DataX.Config.Test
{
    internal class LoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
            throw new System.NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var result = new CacheLogger();
            return result;
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
