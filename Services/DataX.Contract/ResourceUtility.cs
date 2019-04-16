// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Linq;
using System.IO;
using System.Reflection;

namespace DataX.Contract
{
    public static class ResourceUtility
    {
        public static string GetEmbeddedResource(Type containingType, string fileName)
        {
            var assembly = Assembly.GetAssembly(containingType);
            string resourceFile = assembly.GetManifestResourceNames().SingleOrDefault(n => n.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase));

            using (var stream = assembly.GetManifestResourceStream(resourceFile))
            {
                using (var reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
        }
    }
}
