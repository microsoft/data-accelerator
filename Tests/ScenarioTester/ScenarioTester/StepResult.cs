// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;

namespace ScenarioTester
{
    /// <summary>
    /// Result of the <see cref="ScenarioStep"/> execution.
    /// </summary>
    public class StepResult
    {
        /// <summary>
        /// True if the scenario step was successful
        /// </summary>
        public readonly bool Success;

        /// <summary>
        /// Description of the step result
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Value of the result.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// If there was an exception while executing the step.
        /// </summary>
        public readonly Exception Exception;

        /// <summary>
        /// Create a step result for the <see cref="ScenarioStep"/>
        /// </summary>
        /// <param name="success">True if the scenario step was successful</param>
        /// <param name="description">Description of the step result</param>
        /// <param name="result">Value of the result</param>
        /// <param name="exception">If there was an exception while executing the step.</param>
        public StepResult(bool success, string description, string result = null, Exception exception = null)
        {
            Success = success;
            Description = description;
            Value = result;
            Exception = exception;
        }
    }

}
