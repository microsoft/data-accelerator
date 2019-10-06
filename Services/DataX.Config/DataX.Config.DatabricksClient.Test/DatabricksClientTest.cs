using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace DataX.Config.DatabricksClient.Test
{
    [TestClass]
    public class DatabricksClientTest
    {
        [TestMethod]
        public void TestParseJobInfoFromDatabricksHttpResult()
        {
            DatabricksHttpResult httpResult = new DatabricksHttpResult
            {
                Content = "{\"job_id\":40,\"run_id\":49,\"number_in_job\":1,\"original_attempt_run_id\":49,\"state\":{\"life_cycle_state\":\"RUNNING\",\"state_message\":\"In run\"}}",
                IsSuccess = true,
                StatusCode = System.Net.HttpStatusCode.OK
            };
            var expectedValue = new SparkJobSyncResult
            {
                ClientCache = JToken.Parse("{\"job_id\": 40,\"run_id\": 49,\"state\": {\"life_cycle_state\": \"RUNNING\",\"state_message\": \"In run\"}}"),
                JobId = "40",
                JobState = JobState.Running,
                Links = null,
                Note = "In run"
            };
            var actualValue = DatabricksClient.ParseJobInfoFromDatabricksHttpResult(httpResult);
            Assert.AreEqual(expectedValue, actualValue, "ParseJobInfoFromDatabricksHttpResult() failed");
        }

        [TestMethod]
        public void TestParseDatabricksJobResult()
        {
            var jobResult = new DatabricksJobResult
            {
                JobId = 40,
                RunId = 49,
                State = new System.Collections.Generic.Dictionary<string, string>()
                {
                    {"life_cycle_state","RUNNING"},
                    {"state_message","In run"}
                }
            };
            var expectedValue = new SparkJobSyncResult
            {
                ClientCache = JToken.Parse("{\"job_id\": 40,\"run_id\": 49,\"state\": {\"life_cycle_state\": \"RUNNING\",\"state_message\": \"In run\"}}"),
                JobId = "40",
                JobState = JobState.Running,
                Links = null,
                Note = "In run"
            };
            var actualValue = DatabricksClient.ParseDatabricksJobResult(jobResult);
            Assert.AreEqual(expectedValue, actualValue, "ParseDatabricksJobResult() failed");
        }

        [TestMethod]
        public void TestParseDatabricksJobState()
        {
            var expectedValue = JobState.Running;
            var actualValue = DatabricksClient.ParseDatabricksJobState("RUNNING");
            Assert.AreEqual(expectedValue, actualValue, "State mismatch when job state is RUNNING");

            expectedValue = JobState.Starting;
            actualValue = DatabricksClient.ParseDatabricksJobState("PENDING");
            Assert.AreEqual(expectedValue, actualValue, "State mismatch when job state is PENDING");

            expectedValue = JobState.Error;
            actualValue = DatabricksClient.ParseDatabricksJobState("INTERNAL_ERROR");
            Assert.AreEqual(expectedValue, actualValue, "State mismatch when job state is INTERNAL_ERROR");

            expectedValue = JobState.Idle;
            actualValue = DatabricksClient.ParseDatabricksJobState("SKIPPED");
            Assert.AreEqual(expectedValue, actualValue, "State mismatch when job state is SKIPPED");

            actualValue = DatabricksClient.ParseDatabricksJobState("TERMINATING");
            Assert.AreEqual(expectedValue, actualValue, "State mismatch when job state is TERMINATING");

            actualValue = DatabricksClient.ParseDatabricksJobState("TERMINATED");
            Assert.AreEqual(expectedValue, actualValue, "State mismatch when job state is TERMINATED");
        }
    }
}
