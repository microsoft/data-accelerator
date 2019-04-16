// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.Flow.CodegenRules
{
    public static class CodeGen
    {
        public static RulesCode GenerateCode(string queryCode, string rulesData, string productName)
        {
            Engine engine = new Engine();

            return engine.GenerateCode(queryCode, rulesData, productName);
        }
    }
}
