using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.Settings
{
    public static class DataXSettingsConstants
    {
        public const string DataX = nameof(DataX);

        public static readonly string ServiceEnvironment = $"{DataX}:{nameof(ServiceEnvironment)}";
    }
}
