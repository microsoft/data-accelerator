// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Utilities.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataX.Flow.SchemaInference.Tests
{
    [TestClass]
    public class SchemaUtilityTests
    {
        [TestMethod]
        public void BlobPathPrefixTest()
        {
            List<Tuple<string, string, string, string>> blobPaths = new List<Tuple<string, string, string, string>>
            {
                new Tuple<string, string, string, string>(
                    @"wasbs://mycontainer@mysa.blob.core.windows.net/mypath/mypath2/mypath3/{yyyy}/{MM}/{dd}",
                    "mycontainer",
                    @"mypath/mypath2/mypath3/",
                    @"mysa.blob.core.windows.net/mycontainer/mypath/mypath2/mypath3/(\w+)/(\w+)/(\w+)"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://mycontainer@mysa.blob.core.windows.net/mypath/mypath2/mypath3/2018/07/12/subfolder/00/test",
                    "mycontainer",
                    @"mypath/mypath2/mypath3/2018/07/12/subfolder/00/test",
                    @"mysa.blob.core.windows.net/mycontainer/mypath/mypath2/mypath3/2018/07/12/subfolder/00/test"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://myoutputs@somesa.blob.core.windows.net/Test/{yyyy-MM-dd}",
                    "myoutputs",
                    @"Test/",
                    @"somesa.blob.core.windows.net/myoutputs/Test/(\w+)-(\w+)-(\w+)"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://myoutputs@somesa.blob.core.windows.net/{yyyy-MM-dd}",
                    "myoutputs",
                    @"",
                    @"somesa.blob.core.windows.net/myoutputs/(\w+)-(\w+)-(\w+)"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://myoutputs@somesa.blob.core.windows.net/Test/{yyyy}/{MM}/{dd}",
                    "myoutputs",
                    @"Test/",
                    @"somesa.blob.core.windows.net/myoutputs/Test/(\w+)/(\w+)/(\w+)"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://myoutputs@somesa.blob.core.windows.net/{yyyy}/{MM}/{dd}",
                    "myoutputs",
                    @"",
                    @"somesa.blob.core.windows.net/myoutputs/(\w+)/(\w+)/(\w+)"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://myoutputs@somesa.blob.core.windows.net/{yyyy}/{MM}/{dd}/Test",
                    "myoutputs",
                    @"",
                    @"somesa.blob.core.windows.net/myoutputs/(\w+)/(\w+)/(\w+)/Test"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://myoutputs@somesa.blob.core.windows.net/{yyyy-MM-dd}/Test",
                    "myoutputs",
                    @"",
                    @"somesa.blob.core.windows.net/myoutputs/(\w+)-(\w+)-(\w+)/Test"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://mycontainer@mysa.blob.core.windows.net/mypath/mypath2/mypath3/{yyyy/MM/dd}",
                    "mycontainer",
                    @"mypath/mypath2/mypath3/",
                    @"mysa.blob.core.windows.net/mycontainer/mypath/mypath2/mypath3/(\w+)/(\w+)/(\w+)"
                ),
                new Tuple<string, string, string, string>(
                    @"wasbs://mycontainer@mysa.blob.core.windows.net/mypath/mypath2/mypath3/{yyyy/MM/dd}/test",
                    "mycontainer",
                    @"mypath/mypath2/mypath3/",
                    @"mysa.blob.core.windows.net/mycontainer/mypath/mypath2/mypath3/(\w+)/(\w+)/(\w+)/test"
                )
            };

            foreach (var blobPath in blobPaths)
            {
                var wasbPath = blobPath.Item1;
                if (!Uri.TryCreate(wasbPath, UriKind.Absolute, out var uri))
                {
                    Assert.Fail("blob path is incorrect");
                }

                var containerName = uri.UserInfo;
                Assert.AreEqual(blobPath.Item2, containerName, "container name is incorrect");

                var path = uri.Host + "/" + uri.UserInfo + uri.LocalPath;
                var pathPattern = BlobHelper.GenerateRegexPatternFromPath(path);
                Assert.AreEqual(blobPath.Item4, pathPattern, "pattern is incorrect");

                var prefix = BlobHelper.ParsePrefix(wasbPath);
                Assert.AreEqual(blobPath.Item3, prefix, "Prefix generation is incorrect");
            }
        }
    }
}
