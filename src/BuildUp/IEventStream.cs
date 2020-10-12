using System;
using System.Collections.Generic;

namespace BuildUp
{
    public interface IEventStream : IObservable<IBuildUpEvent>
    {
        void PublishEvent(IBuildUpEvent @event);
        void BeginStream();
        void EndStream();
    }

    public interface IBuildUpEvent
    {
        Guid StreamId { get; }
        Type EventType { get; }
        object Data { get; }
        DateTime EventDate { get; }
        int Version { get; }
    }
}