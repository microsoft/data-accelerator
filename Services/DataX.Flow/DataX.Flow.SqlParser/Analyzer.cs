// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DataX.Flow.SqlParser
{
    public static class Analyzer
    {
        public static JArray Analyze(string userCode, string codeGen, string inputSchema)
        {
            SqlParser parser = new SqlParser();
            List<TableData> td = parser.Analyze(userCode, codeGen, inputSchema);
            return JArray.FromObject(td);
        }
    }
}
