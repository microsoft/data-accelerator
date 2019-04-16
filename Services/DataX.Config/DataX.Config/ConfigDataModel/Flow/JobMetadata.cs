// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.Templating;
using System.Collections.Generic;

namespace DataX.Config
{
    public class JobMetadata
    {
        public const string TokenName_JobName = "jobName";
        public static readonly string TokenPlaceHolder_JobName = TokenReplacement.TokenPlaceHolder(TokenName_JobName);

        public JobMetadata(TokenDictionary jobTokens, TokenDictionary commonTokens)
        {
            Tokens = commonTokens.Clone();
            Tokens.AddDictionary(jobTokens);
        }

        public TokenDictionary Tokens { get; }

        public static string GetJobName(TokenDictionary tokens)
        {
            return tokens.GetString(TokenName_JobName);
        }
    }
}
