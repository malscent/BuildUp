using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuildUp.Models;

namespace BuildUp
{
    public interface ISnapshotStorage
    {
        Task StoreSnapshot<T>(Guid streamId, T snapshot, int version) where T : new();
        Task StoreSnapshot(Guid streamId, IBuildUpSnapshot snapshot);
        Task StoreSnapshots(Guid streamId, IEnumerable<IBuildUpSnapshot> snapshots);
    }
}