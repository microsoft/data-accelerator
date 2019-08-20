// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;

namespace JobRunner
{
    /// <summary>
    /// Custom Exception for JobRunner
    /// </summary>
    public class JobRunnerException : Exception
    {
        public JobRunnerException(string message) : base(message)
        {
        }
    }
}
