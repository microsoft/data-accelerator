// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DataX.Flow.CodegenRules.Tests
{
    [TestClass]
    public class CodegenTests
    {
        [TestMethod]
        public void RulesandAlertsTest()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("usercode.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P1");

            StreamReader sr5 = new StreamReader("cgen.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }


        [TestMethod]
        public void RulesandAlertsTestWithDefaultTemplate()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("usercode.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, "P1");

            StreamReader sr5 = new StreamReader("cgenDefault.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }


        [TestMethod]
        public void SimpleRulesTestForDerivedTable()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeSimRulDerTable.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P5");

            StreamReader sr5 = new StreamReader("CGenSimRulDerTable.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void SimpleAlertRulesTestForDerivedTable()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeSimAlDerTable.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P5");

            StreamReader sr5 = new StreamReader("CGenSimAlDerTable.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void AggregateRulesTestForDerivedTable()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeAggRulDerTable.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P5");

            StreamReader sr5 = new StreamReader("CGenAggRulDerTable.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }


        [TestMethod]
        public void AggregateAlertRulesTestForDerivedTable()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeAggAlDerTable.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P5");

            StreamReader sr5 = new StreamReader("CGenAggAlDerTable.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void SimpleRulesTestForNonTable()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeSimRulNonTable.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P1");

            StreamReader sr5 = new StreamReader("CGenSimRulNonTable.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void AggregateAlertAndRuleWithDot()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeAggWithDot.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P2");

            StreamReader sr5 = new StreamReader("CGenAggWithDot.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void SimpleAlertAndRuleWithDot()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeSimWithDot.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P3");

            StreamReader sr5 = new StreamReader("CGenSimWithDot.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void AlertAndRuleWithTick()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeWithTick.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P4");

            StreamReader sr5 = new StreamReader("CGenWithTick.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void NoCodeAlertsTest()
        {
            Engine engine = new Engine();

            string code = "";

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P1");

            StreamReader sr5 = new StreamReader("CGenNoCode.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }


        [TestMethod]
        public void MixedAlertTest()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeAggWithDot.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P6");

            StreamReader sr5 = new StreamReader("CGenMixedAlert.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void MixedAlertWithTickTest()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeAggWithDot.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P6.1");

            StreamReader sr5 = new StreamReader("CGenMixedAlertWithTick.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void CreateMetricTest()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeCreateMetric.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P7");

            StreamReader sr5 = new StreamReader("CGenCreateMetric.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }

        [TestMethod]
        public void CreateMetricTest2()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeCreateMetric2.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P4");

            StreamReader sr5 = new StreamReader("CGenCreateMetric2.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }


        [TestMethod]
        public void NoPivotsTest()
        {
            Engine engine = new Engine();

            string code = "T1 = ProcessAggregateRules(DataXProcessedInput)";

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "P8");

            StreamReader sr5 = new StreamReader("CGenNoPivots.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }


        [TestMethod]
        public void IoTSampleTest()
        {
            Engine engine = new Engine();

            StreamReader sr = new StreamReader("UserCodeIoTSample.txt");
            string code = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("Rules.json");
            string rules = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("QueryTemplates.xml");
            string queryTemplates = sr3.ReadToEnd();
            sr3.Close();

            StreamReader sr4 = new StreamReader("OutputTemplates.xml");
            string outputTemplates = sr4.ReadToEnd();
            sr4.Close();

            RulesCode result = engine.GenerateCode(code, rules, queryTemplates, outputTemplates, "iotsample");

            StreamReader sr5 = new StreamReader("CGenIoTSample.txt");
            string expectedCodegen = sr5.ReadToEnd();
            sr5.Close();

            Assert.AreEqual(result.Code, expectedCodegen, "Codegen is not as expected");
        }
    }
}
