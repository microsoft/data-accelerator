// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ScenarioTester
{
    /// <summary>
    /// Describes a scenario of different "actions" sharing the same context.
    /// </summary>
    public class ScenarioDescription
    {
        /// <summary>
        /// Description of the scenario
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Create a Scenario description from a sequence of actions
        /// </summary>
        /// <param name="description">a description of the scenario</param>
        /// <param name="steps">sequence of steps to execute</param>
        public ScenarioDescription(string description, params ScenarioStep[] steps)
        {
            this.Description = description;
            this.Steps = steps;
        }

        /// <summary>
        /// Sequence of steps defining a scenario
        /// </summary>
        public IReadOnlyCollection<ScenarioStep> Steps { get; }

        /// <summary>
        /// Create <see cref="ScenarioDescription"/> from <paramref name="json"/> using 
        /// steps defined in <paramref name="types"/>.
        /// </summary>
        /// <returns>A scenario description of steps in json</returns>
        public static ScenarioDescription FromJson(string description, string json, params Type[] types)
        {
            var definedSteps = GetStepDefinitions(types);
            var steps = JsonConvert.DeserializeObject<StepDefinition[]>(json);

            var scenarioSteps = new List<ScenarioStep>();
            for (int i = 0; i < steps.Length; i++)
            {
                var found = definedSteps.FirstOrDefault(
                    s => s.GetMethodInfo().GetCustomAttribute<StepAttribute>().Name == steps[i].Action);

                if (found == null)
                {
                    throw new Exception($"{steps[i].Action} not found in Step definitions");
                }
                scenarioSteps.Add(found);
            }

            var scenario = new ScenarioDescription(description, scenarioSteps.ToArray());
            return scenario;
        }

        private static IEnumerable<ScenarioStep> GetStepDefinitions(params Type[] types)
        {
            return types
                    .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    .Where(y => y.GetCustomAttributes().OfType<StepAttribute>().Any())
                    .Select(method => (ScenarioStep)Delegate.CreateDelegate(typeof(ScenarioStep), method));
        }

        private class StepDefinition
        {
            [JsonProperty("action")]
            public string Action;
        }
    }
}
