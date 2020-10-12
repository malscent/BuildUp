using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildUp.InMemoryImplementation
{
    public class NullStorage : ISnapshotStorage, IEventStorage
    {
        public Task StoreSnapshot<T>(Guid streamId, T snapshot)
        {
            return Task.CompletedTask;
        }
        
        public Task<Guid> CreateStream()
        {
            return Task.FromResult(Guid.NewGuid());
        }

        public Task StoreEvents(params IBuildUpEvent[] events)
        {
            return Task.CompletedTask;
        }

        public Task StoreSnapshot<T>(Guid streamId, T snapshot, int version) where T : new()
        {
            return Task.CompletedTask;
        }

        public Task StoreSnapshot(Guid streamId, IBuildUpSnapshot snapshot)
        {
            return Task.CompletedTask;
        }

        public Task StoreSnapshots(Guid streamId, IEnumerable<IBuildUpSnapshot> snapshots)
        {
            return Task.CompletedTask;
        }
    }
}