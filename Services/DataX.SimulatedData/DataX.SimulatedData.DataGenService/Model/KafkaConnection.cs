using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.SimulatedData.DataGenService.Model
{
    public class KafkaConnection
    {
        public string Broker { get; set; }
        public List<string> Topics { get; set; }
        public string ConnectionString { get; set; }
    }
}
