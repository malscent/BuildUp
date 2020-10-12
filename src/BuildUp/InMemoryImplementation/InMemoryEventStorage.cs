using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildUp.Models;

namespace BuildUp.InMemoryImplementation
{
    public class InMemoryEventStorage : IEventStorage, IEventProvider
    {
        private Dictionary<Guid, List<IBuildUpEvent>> _events = new Dictionary<Guid, List<IBuildUpEvent>>();
        
        public Task<Guid> CreateStream()
        {
            var streamId = Guid.NewGuid();
            _events.Add(streamId, new List<IBuildUpEvent>());
            return Task.FromResult(streamId);
        }

        public Task StoreEvents(params IBuildUpEvent[] events)
        {
            foreach (var @event in events)
            {
                if (_events.ContainsKey(@event.StreamId))
                {
                    _events[@event.StreamId].Add(@event);
                }
                else
                {
                    _events.Add(@event.StreamId, new List<IBuildUpEvent>());
                    StoreEvents(@event);
                }
            }
            return Task.CompletedTask;
        }

        public Task<IEventPage> GetEventPage(Guid streamId, int version, int pageSize)
        {
            if (!_events.ContainsKey(streamId))
            {
                throw new KeyNotFoundException("Stream Id does not exist");
            }

            var events = _events[streamId].Where(t => t.Version <= version).Take(pageSize).ToList();
           
            IEventPage page = new EventPage
            {
                Count = events.Count,
                Total = _events[streamId].Count,
                MaxVersion = _events[streamId].Max(t => t.Version),
                Events = events
            };
            return Task.FromResult(page);
        }

        public Task<IEventPage> GetEventPage(Guid streamId, DateTime asOf, int pageSize)
        {
            if (!_events.ContainsKey(streamId))
            {
                throw new KeyNotFoundException("Stream Id does not exist");
            }

            var events = _events[streamId].Where(t => t.EventDate <= asOf).Take(pageSize).ToList();
            var maxVersion = _events[streamId].Where(t => t.EventDate <= asOf)
                                                   .OrderByDescending(t => t.EventDate)
                .FirstOrDefault()?.Version;
            IEventPage page = new EventPage
            {
                Count = events.Count,
                Total = _events[streamId].Count,
                MaxVersion = maxVersion ?? 0,
                Events = events
            };
            return Task.FromResult(page);
        }
    }
}