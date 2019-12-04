// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Utility.KeyVault;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataX.Utilities.Tests
{
    [TestClass]
    public class KeyVaultUtilityTests
    {
        [TestMethod]
        public void KeyVaultParseSecretUriTest()
        {
            SecretUriParser.ParseSecretUri("keyvault://somekeyvalut/test-input-connectionstring-CD42404D52AD55CCFA9ACA4ADC828AA5", out string keyvault, out string secret);

            Assert.AreEqual(keyvault, "somekeyvalut");
            Assert.AreEqual(secret, "test-input-connectionstring-CD42404D52AD55CCFA9ACA4ADC828AA5");

            SecretUriParser.ParseSecretUri("secretscope://somekeyvalut/test-input-connectionstring", out keyvault, out secret);
            Assert.AreEqual(keyvault, "somekeyvalut");
            Assert.AreEqual(secret, "test-input-connectionstring");
        }
}
}
