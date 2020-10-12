using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildUp
{
    public interface IEventStorage
    {
        Task<Guid> CreateStream();
        Task StoreEvents(params IBuildUpEvent[] events);
    }

    public interface IEventProvider
    {
        Task<IEventPage> GetEventPage(Guid streamId, int version, int pageSize);
        Task<IEventPage> GetEventPage(Guid streamId, DateTime asOf, int pageSize);
    }

    public interface IEventPage
    {
        int Count { get; }
        int Total { get; }
        int MaxVersion { get; }
        IEnumerable<IBuildUpEvent> Events { get; }
    }
}