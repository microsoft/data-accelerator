// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.LivyClient
{
    public interface ILivyHttpClientFactory
    {
        ILivyHttpClient CreateClientWithBasicAuth(string userName, string password);
    }
}
