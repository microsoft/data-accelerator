// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;

namespace ScenarioTester
{
    /// <summary>
    /// Identifies a scenario step method. Must be static <see cref="ScenarioStep"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class StepAttribute : Attribute
    {
        public StepAttribute(string stepName)
        {
            this.Name = stepName;
        }

        /// <summary>
        /// Step name
        /// </summary>
        public string Name { get; }
    }

    public delegate StepResult ScenarioStep(ScenarioContext context);
}
