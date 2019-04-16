// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;

namespace DataX.Config.Local
{
    [Shared]
    [Export(typeof(ILogger))]
    public class ConsoleLogger : ILogger
    {
        private class LoggerScope<TState> : IDisposable
        {
            public LoggerScope(TState state)
            {
            }

            public void Dispose()
            {
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new LoggerScope<TState>(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void WriteLine(string msg)
        {
            System.Console.WriteLine(msg);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            WriteLine($"{logLevel}-{eventId}:{formatter(state, exception)}");

            if (exception != null)
            {
                WriteLine(exception.ToString());
            }
        }
    }
}
