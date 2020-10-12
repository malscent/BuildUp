using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildUp
{
    public interface ISnapshotProvider
    {
        Task<T> RetrieveSnapshot<T>(Guid streamId) where T : class, new();
        Task<IEnumerable<IBuildUpSnapshot>> GetSnapshots(Guid streamId);
    }
}