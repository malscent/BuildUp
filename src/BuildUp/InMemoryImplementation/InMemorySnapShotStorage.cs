using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildUp.Models;

namespace BuildUp.InMemoryImplementation
{
    public class InMemorySnapShotStorage : ISnapshotStorage, ISnapshotProvider
    {
        private readonly Dictionary<Guid, Dictionary<Type, List<IBuildUpSnapshot>>> _snapshots = 
            new Dictionary<Guid, Dictionary<Type, List<IBuildUpSnapshot>>>();
        
        public Task<T> RetrieveSnapshot<T>(Guid streamId) where T : class, new()
        {
            if (_snapshots.ContainsKey(streamId) && _snapshots[streamId].ContainsKey(typeof(T)))
            {
                var ss = _snapshots[streamId][typeof(T)].OrderBy(x => x.Version).LastOrDefault();
                if (ss != null)
                {
                    return Task.FromResult((T)ss.Snapshot);
                }
            }
            return Task.FromResult(new T());
        }

        public Task<IEnumerable<IBuildUpSnapshot>> GetSnapshots(Guid streamId)
        {
            var ss = new List<IBuildUpSnapshot>();
            if (_snapshots.ContainsKey(streamId))
            {
                foreach (var typeSnapShots in _snapshots[streamId])
                {
                    ss.AddRange(typeSnapShots.Value);
                }
            }
            return Task.FromResult<IEnumerable<IBuildUpSnapshot>>(ss);
        }

        public Task StoreSnapshot<T>(Guid streamId, T snapshot, int version) where T : new()
        {
            var buildUpSnapshot = new BuildUpSnapshot<T>(snapshot, version);
            if (_snapshots.ContainsKey(streamId))
            {
                if (_snapshots[streamId].ContainsKey(typeof(T)))
                {
                    var ss = _snapshots[streamId][typeof(T)].FirstOrDefault(t => t.Version == version);
                    if (ss == null)
                    {
                        _snapshots[streamId][typeof(T)].Add(buildUpSnapshot);
                    }
                    else
                    {
                        ss = buildUpSnapshot;
                    }
                }
                else
                {
                    _snapshots[streamId].Add(typeof(T), new List<IBuildUpSnapshot> { buildUpSnapshot });
                }
            }
            else
            {
                _snapshots.Add(streamId, new Dictionary<Type, List<IBuildUpSnapshot>>
                {
                    { typeof(T), new List<IBuildUpSnapshot> { buildUpSnapshot } }
                });
            }
            return Task.CompletedTask;
        }

        public Task StoreSnapshot(Guid streamId, IBuildUpSnapshot snapshot)
        {
            if (_snapshots.ContainsKey(streamId))
            {
                if (_snapshots[streamId].ContainsKey(snapshot.ProjectionType))
                {
                    var ss = _snapshots[streamId][snapshot.ProjectionType]
                                                .FirstOrDefault(t => t.Version == snapshot.Version);
                    if (ss == null)
                    {
                        _snapshots[streamId][snapshot.ProjectionType].Add(snapshot);
                    }
                    else
                    {
                        ss = snapshot;
                    }
                }
                else
                {
                    _snapshots[streamId].Add(snapshot.ProjectionType, new List<IBuildUpSnapshot> { snapshot });
                }
            }
            else
            {
                _snapshots.Add(streamId, new Dictionary<Type, List<IBuildUpSnapshot>>
                {
                    { snapshot.ProjectionType, new List<IBuildUpSnapshot> { snapshot } }
                });
            }
            return Task.CompletedTask;
        }

        public Task StoreSnapshots(Guid streamId, IEnumerable<IBuildUpSnapshot> snapshots)
        {
            foreach (var ss in snapshots)
            {
                StoreSnapshot(streamId, ss);
            }
            return Task.CompletedTask;
        }
    }
}