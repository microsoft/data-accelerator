// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using DataX.Contract.Exception;

namespace DataX.Contract
{
    public static class Ensure
    {
        public static void Throw(string msg)
        {
            throw new GeneralException(msg);
        }

        public static void NotNull<T>(T value, string varName, params string[] additionals)
        {
            if (value == null)
            {
                Throw($"variable '{varName}' cannot be null! {string.Join("", additionals)}");
            }
        }

        public static void NotNull<T>(T value, string varName, Func<string> msgGenerator)
        {
            if (value == null)
            {
                Throw($"variable '{varName}' cannot be null! {msgGenerator()}");
            }
        }

        public static void IsSuccessResult(Result result)
        {
            if (!result.IsSuccess)
            {
                throw new GeneralException(result.Message);
            }
        }

        public static void EnsureNullElseThrowNotSupported(object o, string msg)
        {
            if (o != null)
            {
                throw new NotSupportedException(msg);
            }
        }
    }
}
