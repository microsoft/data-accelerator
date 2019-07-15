// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using ScenarioTester;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace TestScenarioTester
{
    [TestClass]
    public class TestScenarioTester
    {
        public class TestHost
        {
            public static StepResult Step1(ScenarioContext context)
            {
                return new StepResult(true, nameof(Step1));
            }

            [Step(nameof(Step2))]
            public static StepResult Step2(ScenarioContext context)
            {
                int sequence = 0;
                if (context.TryGetValue("sequence", out object sequenceObj))
                {
                    sequence = (int)sequenceObj;
                }
                context[nameof(Step2)] = sequence++;
                context["sequence"] = sequence;

                return new StepResult(true, nameof(Step2));
            }

            [Step(nameof(FailedStep))]
            public static StepResult FailedStep(ScenarioContext context)
            {
                int sequence = 0;
                if (context.TryGetValue("sequence", out object sequenceObj))
                {
                    sequence = (int)sequenceObj;
                }
                context[nameof(FailedStep)] = sequence++;
                context["sequence"] = sequence;

                return new StepResult(false, nameof(FailedStep));
            }
        }

        [TestMethod]
        public void CreateScenarioFromJson()
        {
            var scenario = ScenarioDescription.FromJson("scenario", @"[{'action':'Step2'}]", typeof(TestHost));
            Assert.AreEqual(1, scenario.Steps.Count);
            Assert.AreEqual(nameof(TestHost.Step2), scenario.Steps.First().Method.Name);
        }

        [TestMethod]
        public void ScenarioFailsWithFailedStep()
        {
            var scenario = new ScenarioDescription("scenario", TestHost.FailedStep, TestHost.Step2);
            var result = new ScenarioResult(scenario);

            result.Run(new ScenarioContext());

            Assert.IsTrue(result.Failed);
        }

        [TestMethod]
        public void ScenarioPassesWithStep()
        {
            var scenario = new ScenarioDescription("scenario", TestHost.Step2);
            var result = new ScenarioResult(scenario);

            result.Run(new ScenarioContext());

            Assert.IsFalse(result.Failed);
        }

        [TestMethod]
        public void ExecutionSequenceByMethod()
        {
            var scenario = new ScenarioDescription("scenario1", TestHost.FailedStep, TestHost.Step2);
            var result = new ScenarioResult(scenario);

            var testContext = new ScenarioContext();
            testContext["sequence"] = 1;
            result.Run(testContext);

            Assert.AreEqual(1, (int)testContext[nameof(TestHost.FailedStep)], scenario.Description);
            Assert.AreEqual(2, (int)testContext[nameof(TestHost.Step2)], scenario.Description);

            // change sequence and make sure that it is different this time
            scenario = new ScenarioDescription("scenario2", TestHost.Step2, TestHost.FailedStep);
            result = new ScenarioResult(scenario);

            testContext = new ScenarioContext();
            testContext["sequence"] = 1;
            result.Run(testContext);

            Assert.AreEqual(2, (int)testContext[nameof(TestHost.FailedStep)], scenario.Description);
            Assert.AreEqual(1, (int)testContext[nameof(TestHost.Step2)], scenario.Description);

        }

        [TestMethod]
        public void ExecutionSequenceByJson()
        {
            var scenario = ScenarioDescription.FromJson("scenario1", @"[{'action':'Step2'}, {'action':'FailedStep'}]", typeof(TestHost));
            var result = new ScenarioResult(scenario);

            var testContext = new ScenarioContext();
            testContext["sequence"] = 1;
            result.Run(testContext);

            Assert.AreEqual(2, (int)testContext[nameof(TestHost.FailedStep)], scenario.Description);
            Assert.AreEqual(1, (int)testContext[nameof(TestHost.Step2)], scenario.Description);

            // change sequence and make sure that it is different this time
            scenario = ScenarioDescription.FromJson("scenario2", @"[{'action':'FailedStep'}, {'action':'Step2'}]", typeof(TestHost));
            result = new ScenarioResult(scenario);

            testContext = new ScenarioContext();
            testContext["sequence"] = 1;
            result.Run(testContext);

            Assert.AreEqual(1, (int)testContext[nameof(TestHost.FailedStep)], scenario.Description);
            Assert.AreEqual(2, (int)testContext[nameof(TestHost.Step2)], scenario.Description);
        }

        [TestMethod]
        public void ContextIsDuplicated()
        {
            var context1 = new ScenarioContext();
            context1["init"] = "init";

            var context2 = new ScenarioContext(context1);

            Assert.AreEqual(1, context2.Count, "count of keys copied over");
            Assert.AreEqual("init", context2["init"], "value copied over");

            context1["newincontext1"] = "newincontext1";

            Assert.AreEqual(1, context2.Count, "count is not changed when context1 changes");
            Assert.IsFalse(context2.ContainsKey("newincontext1"), "new key in context1 is not in context2");

            context2["newincontext2"] = "newincontext2";

            Assert.IsFalse(context1.ContainsKey("newincontext2"), "new key in context2 is not in context1");

            context2["init"] = "changed";

            Assert.AreEqual("changed", context2["init"], "initial keys updated when changed in context");
            Assert.AreEqual("init", context1["init"], "initial keys not updated when changed in another context");
        }
    }
}
