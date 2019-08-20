// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System.Threading.Tasks;

namespace JobRunner.Jobs
{
    public interface IJob
    {
        Task RunAsync();
    }
}
