// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ScenarioTester
{
    /// <summary>
    /// Shared context for all the scenario steps. Could be used to pass in parameters
    /// as input and output for steps.
    /// </summary>
    public class ScenarioContext : ConcurrentDictionary<string, object>, IDisposable
    {
        public ScenarioContext() : base() { }

        /// <summary>
        /// Copy another context
        /// </summary>
        public ScenarioContext(ScenarioContext context)
            : base(context.ToDictionary(c => c.Key, c => c.Value)) { }

        /// <summary>
        /// Dispose of all disposable properties.
        /// </summary>
        public void Dispose()
        {
            foreach (var value in this.Values)
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
