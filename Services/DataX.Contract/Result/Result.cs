// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Contract
{
    public class Result
    {
        public bool IsSuccess;
        public string Message;
        public Dictionary<string, object> Properties;
    }
}
