// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataX.Config.Test
{
    [TestClass]
    public class SparkJobOperationTest
    {
        [TestMethod]
        public void TestVerifyJobStoppedt()
        {
            JobState errorState = JobState.Error;
            JobState idleState = JobState.Idle;
            JobState runningState = JobState.Running;
            JobState startingState = JobState.Starting;
            JobState successState = JobState.Success;

            //Verify error state is considered that the job is not running
            Assert.IsTrue(SparkJobOperation.VerifyJobStopped(errorState));

            //Verify idle state is considered that the job is not running
            Assert.IsTrue(SparkJobOperation.VerifyJobStopped(idleState));

            //Verify running state is considered that the job is running
            Assert.IsFalse(SparkJobOperation.VerifyJobStopped(runningState));

            //Verify starting state is considered that the job is running
            Assert.IsFalse(SparkJobOperation.VerifyJobStopped(startingState));

            //Verify success state is considered that the job is not running
            Assert.IsTrue(SparkJobOperation.VerifyJobStopped(successState));
        }
    }
}
