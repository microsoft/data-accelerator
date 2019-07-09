// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Flow.InteractiveQuery
{
    /// <summary>
    /// Interface for the Spark kernel
    /// </summary>
    public interface IKernel
    {
        /// <summary>
        /// Id of the kernel
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Executes the code in Spark cluster
        /// </summary>
        /// <param name="code">Code to execute</param>
        /// <returns>Results of code execution</returns>
        string ExecuteCode(string code);
    }
   
}
