// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Test.Extension
{
    public static class CommonDataExtension
    {
        public static async Task Add(this ICommonDataManager commonData, string name , string filePath)
        {
            await commonData.SaveByName(name, JsonConfig.From(await File.ReadAllTextAsync(filePath)));
        }
    }
}
