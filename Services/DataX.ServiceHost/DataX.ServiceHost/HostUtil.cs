using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost
{
    public static class HostUtil
    {
        public static bool InServiceFabric => Environment.GetEnvironmentVariable("Fabric_ApplicationName") != null;
    }
}
