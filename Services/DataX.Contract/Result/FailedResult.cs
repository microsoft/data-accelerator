// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.Contract
{
    public class FailedResult : Result
    {
        public FailedResult(string msg)
        {
            this.IsSuccess = false;
            this.Message = msg;
        }
    }
}
