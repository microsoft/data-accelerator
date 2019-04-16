// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Test.Utility.Mock
{
    [Shared]
    [Export]
    [Export(typeof(ILogger))]
    public class CacheLogger : ILogger
    {
        public CacheLogger()
        {
            Logs = new List<string>();
        }

        public List<string> Logs { get; }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logs.Add($"{logLevel}-{eventId}: {formatter(state, exception)}");
        }
    }
}
