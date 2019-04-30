using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference
{
    public interface IMessageBus
    {
        Task<EventsData> GetSampleEvents(int seconds);
    }
}
