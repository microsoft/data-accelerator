// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;
using System.Threading.Tasks;

namespace JobRunner.Jobs
{
    /// <summary>
    /// An example job that shows how a job could be setup
    /// </summary>
    public class TestJob : IJob
    {
        public TestJob()
        {
        }

        public async Task RunAsync()
        {
            Console.WriteLine("TESTJOB");
            await Task.Yield();
        }
    }
}
