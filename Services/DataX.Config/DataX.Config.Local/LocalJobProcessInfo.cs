// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.Local
{
    public class LocalJobProcessInfo
    {
        public int PId{ get; set; }
        public string Name { get; set; }
        public IntPtr Handle { get; set; }
    }
}
