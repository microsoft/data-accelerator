// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.Contract.Settings
{
    /// <summary>
    /// Constants used with <see cref="DataXSettings"/>
    /// </summary>
    public static class DataXSettingsConstants
    {
        public const string DataX = nameof(DataX);

        public static readonly string ServiceEnvironment = $"{DataX}:{nameof(ServiceEnvironment)}";
    }
}
