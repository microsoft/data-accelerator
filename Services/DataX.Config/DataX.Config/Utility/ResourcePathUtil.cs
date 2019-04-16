// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.Utility
{
    public static class ResourcePathUtil
    {
        public static string Combine(string folder, string file)
        {
            if (folder == null)
            {
                return file;
            }

            if (file == null)
            {
                return folder;
            }

            return folder.TrimEnd('/') + '/' + file;
        }
    }
}
