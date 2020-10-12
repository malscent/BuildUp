using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildUp.InMemoryImplementation;
using BuildUp.Models;
using Xunit;

namespace BuildUp.Test
{
    public class EventStreamListening
    {
        public class RandomEvent
        {
            public int I { get; set; }
        }

        public class RandomProjection
        {
            public int I { get; set; }

            public RandomProjection Apply(RandomProjection proj, RandomEvent @event)
            {
                proj.I = @event.I;
                return proj;
            }
        }
        [Fact]
        public async Task CanSpecifyEventStreamAndPublishEvent()
        {
            // SETUP
            var es = new InMemoryEventStream();
            var eStorage = new InMemoryEventStorage();
            var sStorage = new InMemorySnapShotStorage();
            var buildup = BuildUp.Initialize(t =>
            {
                t.SetEventStream(es);
                t.SetEventProvider(eStorage);
                t.SetEventStorage(eStorage);
                t.SetSnapshotProvider(sStorage);
                t.SetSnapshotStorage(sStorage);
                t.RegisterProjection<RandomProjection>().CreatedBy<RandomEvent>();
            });
            var @event = new BuildUpEvent<RandomEvent>
            {
                StreamId = Guid.NewGuid(),
                Data = new RandomEvent {I = 5},
                EventDate = DateTime.UtcNow,
                Version = 1
            };
            //ACT
            es.PublishEvent(@event);
            es.EndStream();
            Thread.Sleep(25);
            //ASSERT
            var projections = await sStorage.GetSnapshots(@event.StreamId);
            var buildUpSnapshots = projections as IBuildUpSnapshot[] ?? projections.ToArray();
            Assert.Single(buildUpSnapshots);
            var projection = (RandomProjection)buildUpSnapshots.First().Snapshot;
            Assert.Equal(5, projection.I);
        }
        
        [Fact]
        public async Task HandlesMultipleEventsOK()
        {
            // SETUP
            var es = new InMemoryEventStream();
            var eStorage = new InMemoryEventStorage();
            var sStorage = new InMemorySnapShotStorage();
            var buildup = BuildUp.Initialize(t =>
            {
                t.SetEventStream(es);
                t.SetEventProvider(eStorage);
                t.SetEventStorage(eStorage);
                t.SetSnapshotProvider(sStorage);
                t.SetSnapshotStorage(sStorage);
            });
            var @event = new BuildUpEvent<RandomEvent>
            {
                StreamId = Guid.NewGuid(),
                Data = new RandomEvent {I = 5},
                EventDate = DateTime.UtcNow,
                Version = 1
            };
            var eventTwo = new BuildUpEvent<RandomEvent>
            {
                StreamId = @event.StreamId,
                Data = new RandomEvent{ I = 72},
                EventDate = DateTime.UtcNow,
                Version = 2
            };
            //ACT
            es.PublishEvent(@event);
            es.PublishEvent(eventTwo);
            es.EndStream();
            Thread.Sleep(25);
            //ASSERT
            var projections = await sStorage.GetSnapshots(@event.StreamId);
            var buildUpSnapshots = projections as IBuildUpSnapshot[] ?? projections.ToArray();
            Assert.Single(buildUpSnapshots);
            var projection = (RandomProjection)buildUpSnapshots.First().Snapshot;
            Assert.Equal(72, projection.I);
        }        
    }
}