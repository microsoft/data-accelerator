// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;
using System.Threading;

namespace DataX.Utilities
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Returns true if an exception should not be handled by logging code.
        /// </summary>
        public static bool IsCriticalException(this Exception ex)
        {
            return ex is StackOverflowException ||
                ex is OutOfMemoryException ||
                ex is ThreadAbortException ||
                ex is AccessViolationException;
        }
    }
}
