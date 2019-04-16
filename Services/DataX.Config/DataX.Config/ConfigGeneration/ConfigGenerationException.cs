// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Exception;
using System;
using System.Runtime.Serialization;

namespace DataX.Config
{
    [Serializable]
    public class ConfigGenerationException : GeneralException
    {
        public ConfigGenerationException()
        {
        }

        public ConfigGenerationException(string message) : base(message)
        {
        }

        public ConfigGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConfigGenerationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
