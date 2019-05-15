using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.Settings
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
