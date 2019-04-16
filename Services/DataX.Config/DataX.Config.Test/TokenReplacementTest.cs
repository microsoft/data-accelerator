// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Config.Templating;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.Test
{
    [TestClass]
    public class TokenReplacementTest
    {
        [TestMethod]
        public void TestResolvingReferencedToken()
        {
            var tokens = new Dictionary<string, Token>()
            {
                {"name", Token.FromString("testName") },
                {"anotherName", Token.FromString(TokenReplacement.TokenPlaceHolder("name")) }
            };

            var resolved = TokenReplacement.ResolveTokens(tokens);

            Assert.AreEqual(expected: "testName", actual: resolved["anotherName"].Value);
        }

        [TestMethod]
        public void TestTokenDictionaryForwardResolution()
        {
            var tokens = new TokenDictionary();
            tokens.Set("name", Token.FromString("testName"));
            tokens.Set("anotherName", Token.FromString(TokenReplacement.TokenPlaceHolder("name")));

            Assert.AreEqual(expected: "testName", actual: tokens.GetString("anotherName"));
        }

        [TestMethod]
        public void TestTokenDictionaryBackwardResolution()
        {
            var tokens = new TokenDictionary();
            tokens.Set("anotherName", Token.FromString(TokenReplacement.TokenPlaceHolder("name")));
            tokens.Set("name", Token.FromString("testName"));

            Assert.AreEqual(expected: "testName", actual: tokens.GetString("anotherName"));
        }

        [TestMethod]
        public void TestTokenDictionaryResolution()
        {
            var tokens = new TokenDictionary();
            tokens.Set("name1", Token.FromString(TokenReplacement.TokenPlaceHolder("name2")));
            tokens.Set("name3", Token.FromString(TokenReplacement.TokenPlaceHolder("name2")));
            tokens.Set("name2", Token.FromString("testName"));

            Assert.AreEqual(expected: "testName", actual: tokens.GetString("name1"));
            Assert.AreEqual(expected: "testName", actual: tokens.GetString("name3"));
        }
    }
}
