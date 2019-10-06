// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

namespace JobRunner
{
    /// <summary>
    /// Message object to specify a job to be run asynchronously
    /// </summary>
    internal class JobQueueMessage
    {
        public string JobKey { get; set; }
    }
}
