// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Collections.Generic;

namespace DataX.Contract
{
    public class SuccessResult : Result
    {
        public SuccessResult(string msg = "", Dictionary<string, object> props = null)
        {
            this.IsSuccess = true;
            this.Message = msg;
            this.Properties = props;
        }
    }
}
