// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace ScenarioTester
{
    /// <summary>
    /// Executes a scenario and provides the results for the scenario. 
    /// </summary>
    public class ScenarioResult
    {
        private static readonly Stopwatch _Watch = Stopwatch.StartNew();

        /// <summary>
        /// True if any of the steps in the scenario execution failed.
        /// </summary>
        public bool Failed { get; private set; }

        public ScenarioResult(ScenarioDescription scenario)
        {
            Scenario = scenario;
        }

        /// <summary>
        /// Result of each step in the <see cref="ScenarioDescription"/>
        /// </summary>
        public List<StepResult> StepResults { get; } = new List<StepResult>();

        /// <summary>
        /// The scenario definition to execute
        /// </summary>
        public ScenarioDescription Scenario { get; }

        /// <summary>
        /// Run the scenario using the given <paramref name="context"/>.
        /// </summary>
        public void Run(ScenarioContext context)
        {
            // TODO: Log the start-end time.
            var startTime = _Watch.ElapsedMilliseconds;

            foreach (var step in this.Scenario.Steps)
            {
                var stepStart = _Watch.ElapsedMilliseconds;

                try
                {
                    var result = step(context);
                    this.StepResults.Add(result);
                    var stepEnd = _Watch.ElapsedMilliseconds;
                    this.Failed |= !result.Success;
                }
                catch (Exception e)
                {
                    this.Failed = true;

                    var stepName = step.Method.GetCustomAttribute<StepAttribute>().Name;
                    this.StepResults.Add(new StepResult(false, stepName, exception: e));
                    var stepEnd = _Watch.ElapsedMilliseconds;
                    // break; Don't fail but continue to allow any cleanups to happen.
                    // TODO: make this configurable
                }
            }

            var endTime = _Watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Run the given <paramref name="scenario"/> for <paramref name="count"/> number of iterations parallalelly.
        /// Each iteration will get their own iteration context that will be shared across the scenario steps.
        /// </summary>
        /// <returns>Collection of all <see cref="ScenarioResult"/>.</returns>
        public static async Task<IEnumerable<ScenarioResult>> RunAsync(ScenarioDescription scenario, ScenarioContext context, int count)
        {
            List<Task> tasks = new List<Task>();
            List<ScenarioResult> results = new List<ScenarioResult>();

            for (int i = 0; i < count; i++)
            {
                using (var iterationContext = new ScenarioContext(context))
                {
                    var result = new ScenarioResult(scenario);
                    results.Add(result);
                    tasks.Add(Task.Run(() => result.Run(iterationContext)));
                }
            }

            await Task.WhenAll(tasks.ToArray());
            return results;
        }
    }
}
