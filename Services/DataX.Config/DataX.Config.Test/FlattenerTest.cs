// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Test
{
    [TestClass]
    public class FlattenerTest
    {
        [TestMethod]
        public async Task TestFlatten()
        {
            var config = await File.ReadAllTextAsync(@"Resource\Flattener\config.json");
            var flattener = ConfigFlattener.From(JsonConfig.From(config));

            var input = await File.ReadAllTextAsync(@"Resource\Flattener\input.json");
            var inputJson = JsonConfig.From(input);

            var output = flattener.Flatten(inputJson);

            var actualConf = PropertiesDictionary.From(output);
            var expected = PropertiesDictionary.From(await File.ReadAllTextAsync(@"Resource\Flattener\output.conf"));

            var matches = PropertiesDictionary.Match(expected, actualConf).ToList();
            foreach(var match in matches)
            {
                Console.WriteLine($"property:'{match.Item1}', expected:'{match.Item2}', actual:'{match.Item3}'");
            }

            foreach (var match in matches)
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"property:'{match.Item1}'");
            }
        }
    }
}
