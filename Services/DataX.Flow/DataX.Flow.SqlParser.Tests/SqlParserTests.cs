// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace DataX.Flow.SqlParser.Tests
{
    [TestClass]
    public class SqlParserTests
    {
        [TestMethod]
        public void ComplexTest()
        {
            StreamReader sr = new StreamReader("usercode.txt");
            string userCode = sr.ReadToEnd();
            sr.Close();

            StreamReader sr2 = new StreamReader("cgen.txt");
            string codegen = sr2.ReadToEnd();
            sr2.Close();

            StreamReader sr3 = new StreamReader("inputschema.json");
            string inputSchema = sr3.ReadToEnd();
            sr3.Close();

            SqlParser parser = new SqlParser();
            List<TableData> td = parser.Analyze(userCode, codegen, inputSchema);

            string actualParserOutput = JsonConvert.SerializeObject(td);

            StreamReader sr4 = new StreamReader("expectedComplexTest.txt");
            string expectedParserOutput = sr4.ReadToEnd();
            sr4.Close();

            Assert.AreEqual(expectedParserOutput, actualParserOutput, "Sql parser test failed");
        }
    }
}
