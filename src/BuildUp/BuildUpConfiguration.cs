using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BuildUp.InMemoryImplementation;
using BuildUp.Models;

namespace BuildUp
{
    internal class BuildUpConfiguration : IBuildUpInitialization, IEventStream, IEventStorage, IEventProvider, ISnapshotProvider, ISnapshotStorage
    {
        private Dictionary<Type, Dictionary<Type, Transform>> _transforms = new Dictionary<Type, Dictionary<Type, Transform>>();
        private Dictionary<Type, Dictionary<Type, ApplyMethod>> _applyMethods = new Dictionary<Type, Dictionary<Type, ApplyMethod>>();
        private List<BuildUpProjection> _projections = new List<BuildUpProjection>();
        private IEventStream _eventStream = null;
        private IEventStorage _eventStore = null;
        private IEventProvider _eventProvider = null;
        private ISnapshotProvider _snapshotProvider = null;
        private ISnapshotStorage _snapshotStorage = null;
        public IApplyMethod RegisterApply(ApplyMethod application)
        {
            if (!_applyMethods.ContainsKey(application.ProjectionType))
            {
                _applyMethods.Add(application.ProjectionType, new Dictionary<Type, ApplyMethod>
                {
                    {application.EventType, application}
                });
            }
            else if (!_applyMethods[application.ProjectionType].ContainsKey(application.EventType))
            {
                _applyMethods[application.ProjectionType].Add(application.EventType, application);
            }
            else
            {
                _applyMethods[application.ProjectionType][application.EventType] = application;
            }

            return application;
        }
        public IApplyMethod RegisterApply<TProjection, TEvent>(Func<TProjection, TEvent, TProjection> apply)
        {
            var application = new ApplyMethod
            {
                ApplyDelegate = apply,
                EventType = typeof(TEvent),
                ProjectionType = typeof(TProjection)
            };
            return RegisterApply(application);
        }

        public void RegisterEventTransform<TOld, TNew>(Func<TOld, TNew> transform)
        {
            var trans = new Transform
            {
                OldType = typeof(TOld),
                NewType = typeof(TNew),
                TransformDelegate = transform
            };
            
            if (!_transforms.ContainsKey(trans.OldType))
            {
                _transforms.Add(trans.OldType, new Dictionary<Type, Transform>
                {
                    {trans.NewType, trans}
                });
            }
            else if (!_transforms[trans.OldType].ContainsKey(trans.NewType))
            {
                _transforms[trans.OldType].Add(trans.NewType, trans);
            }
            else
            {
                _transforms[trans.OldType][trans.NewType] = trans;
            }
        }

        private readonly Func<object, object> _baseDelegate = (o) => o;

        public ApplyMethod FindApplyMethod(Type projectionType, Type eventType)
        {
            if (_applyMethods.ContainsKey(projectionType) && _applyMethods[projectionType].ContainsKey(eventType))
            {
                return _applyMethods[projectionType][eventType];
            }
            return null;
        }

        public Dictionary<Type, ApplyMethod> GetApplyMethods(Type projectionType)
        {
            if (_applyMethods.ContainsKey(projectionType))
            {
                return _applyMethods[projectionType];
            }
            else
            {
                return new Dictionary<Type, ApplyMethod>();
            }
        }

        public List<ApplyMethod> GetApplyMethods()
        {
            return _applyMethods.Values.SelectMany(t => t.Values).ToList();
        }

        public bool IsProjectable(Type projectionType)
        {
            return _applyMethods.ContainsKey(projectionType);
        }

        public bool IsTransformable(Type eventType)
        {
            return _transforms.ContainsKey(eventType);
        }

        public IBuildUpProjection RegisterProjection<T>() where T : new()
        {
            var projectionType = typeof(T);
            var instance = new T();
            var projectionMethods =
                projectionType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            var applyMethods = new List<ApplyMethod>();
            foreach (var method in projectionMethods)
            {
                var methodParameters = method.GetParameters();
                if (methodParameters.Length != 2)
                {
                    continue;
                }

                if (methodParameters[0].ParameterType != projectionType)
                {
                    continue;
                }

                if (method.ReturnType != projectionType)
                {
                    continue;
                }

                var eventType = methodParameters[1].ParameterType;
                var appMethod = new ApplyMethod
                {
                    ProjectionType = projectionType,
                    EventType = eventType,
                    ApplyDelegate = CreateDelegateFromMethodInfo(projectionType, eventType, method, instance)
                };
                applyMethods.Add(appMethod);
                this.RegisterApply(appMethod);
            }
            var projection = new BuildUpProjection(projectionType, applyMethods);
            _projections.Add(projection);
            return projection;
        }

        public void SetEventStream(IEventStream stream)
        {
            _eventStream = stream;
        }

        public void SetEventStorage(IEventStorage store)
        {
            _eventStore = store;
        }

        public void SetEventProvider(IEventProvider provider)
        {
            _eventProvider = provider;
        }

        public void SetSnapshotProvider(ISnapshotProvider provider)
        {
            _snapshotProvider = provider;
        }

        public void SetSnapshotStorage(ISnapshotStorage store)
        {
            _snapshotStorage = store;
        }

        internal List<BuildUpProjection> GetProjections() => _projections;

        private Delegate CreateDelegateFromMethodInfo(Type projectionType, Type eventType, MethodInfo method, object target)
        {
            Delegate delgato;
            var types = method.GetParameters().Select(p => p.ParameterType);
            types = types.Concat(new[] {method.ReturnType});
            delgato = method.IsStatic ? 
                Delegate.CreateDelegate(Expression.GetFuncType(types.ToArray()), method) : 
                Delegate.CreateDelegate(Expression.GetFuncType(types.ToArray()), target, method.Name);
            return delgato;
        }

        public Delegate FindTransform(Type projectionType, Type baseEventType)
        {
            if (!_transforms.ContainsKey(baseEventType))
            {
                return _baseDelegate;
            }
            var transforms = _transforms[baseEventType];
            
            // Do we have a direct transform to an event with an apply method
            foreach (var transform in transforms)
            {
                if (_applyMethods[projectionType].ContainsKey(transform.Key))
                {
                    return transform.Value.TransformDelegate;
                }
            }

            var findTransform = FindDelegate(projectionType, baseEventType, transforms);
            CacheTransform(findTransform);
            if (findTransform != null) return findTransform.TransformDelegate;
            throw new NotImplementedException();
        }

        private void CacheTransform(Transform findTransform)
        {
            var method = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(t => t.Name == "RegisterEventTransform");
            if (method is { })
            {
                var generic = method.MakeGenericMethod(findTransform.OldType, findTransform.NewType);
                generic.Invoke(this, new object[] {findTransform.TransformDelegate});
            }
        }

        private Transform FindDelegate(Type projectionType, Type oldType, Dictionary<Type, Transform> transforms)
        {
            var dm = new Transform
            {
                OldType = oldType
            };
            foreach (var transform in transforms)
            {
                var delegates = new List<Delegate>();
                bool reachedApply = false;
                if (_transforms.ContainsKey(transform.Key))
                {
                    delegates.Add(transform.Value.TransformDelegate);
                    foreach (var subTransform in _transforms[transform.Key])
                    {
                        if (_applyMethods[projectionType].ContainsKey(subTransform.Key))
                        {
                            delegates.Add(subTransform.Value.TransformDelegate);
                            dm.NewType = subTransform.Key;
                            reachedApply = true;
                            break;
                        }
                        if (_transforms.ContainsKey(subTransform.Key))
                        {
                            var del = FindDelegate(projectionType, subTransform.Key, _transforms[subTransform.Key]);
                            if (del != null)
                            {
                                reachedApply = true;
                                dm.NewType = del.NewType;
                                delegates.Add(del.TransformDelegate);
                            }
                        }
                    }
                }

                if (reachedApply)
                {
                    var mInfo = this.GetType().GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).FirstOrDefault(x => x.Name == "GenerateDelegate");
                    if (mInfo != null)
                    {
                        var generic = mInfo.MakeGenericMethod(oldType, dm.NewType);
                        dm.TransformDelegate =
                            (Delegate) generic.Invoke(this,new object[] {delegates, oldType, dm.NewType});
                    }
                    return dm;
                }
            }

            return null;
        }

        private Delegate GenerateDelegate<TOld, TNew>(List<Delegate> delegates, Type oldType, Type newType)
        {
            Func<TOld, TNew> del = old =>
            {
                object o = old;
                foreach (var deleg in delegates)
                {
                    o = deleg.DynamicInvoke(o);
                }
                return (TNew)o;
            };
            return del;
        }
#region CompositionMethods
        public IDisposable Subscribe(IObserver<IBuildUpEvent> observer)
        {
            _eventStream ??= new InMemoryEventStream();
            return _eventStream.Subscribe(observer);
        }

        public void PublishEvent(IBuildUpEvent @event)
        {
            _eventStream ??= new InMemoryEventStream();
            _eventStream.PublishEvent(@event);
        }

        public void BeginStream()
        {
            _eventStream ??= new InMemoryEventStream();
            _eventStream.BeginStream();
        }

        public void EndStream()
        {
            _eventStream ??= new InMemoryEventStream();
            _eventStream.EndStream();
        }

        public Task<Guid> CreateStream()
        {
            _eventStore ??= new InMemoryEventStorage();
            return _eventStore.CreateStream();
        }

        public Task StoreEvents(params IBuildUpEvent[] events)
        {
            _eventStore ??= new InMemoryEventStorage();
            return _eventStore.StoreEvents(events);
        }

        public Task<IEventPage> GetEventPage(Guid streamId, int version, int pageSize)
        {
            _eventProvider ??= new InMemoryEventStorage();
            return _eventProvider.GetEventPage(streamId, version, pageSize);
        }

        public Task<IEventPage> GetEventPage(Guid streamId, DateTime asOf, int pageSize)
        {
            _eventProvider ??= new InMemoryEventStorage();
            return _eventProvider.GetEventPage(streamId, asOf, pageSize);
        }

        public Task<T> RetrieveSnapshot<T>(Guid streamId) where T : class, new()
        {
            _snapshotProvider ??= new InMemorySnapShotStorage();
            return _snapshotProvider.RetrieveSnapshot<T>(streamId);
        }

        public Task<IEnumerable<IBuildUpSnapshot>> GetSnapshots(Guid streamId)
        {
            _snapshotProvider ??= new InMemorySnapShotStorage();
            return _snapshotProvider.GetSnapshots(streamId);
        }

        public Task StoreSnapshot<T>(Guid streamId, T snapshot, int version) where T : new()
        {
            _snapshotStorage ??= new InMemorySnapShotStorage();
            return _snapshotStorage.StoreSnapshot<T>(streamId, snapshot, version);
        }

        public Task StoreSnapshot(Guid streamId, IBuildUpSnapshot snapshot)
        {
            _snapshotStorage ??= new InMemorySnapShotStorage();
            return _snapshotStorage.StoreSnapshot(streamId, snapshot);
        }

        public Task StoreSnapshots(Guid streamId, IEnumerable<IBuildUpSnapshot> snapshots)
        {
            _snapshotStorage ??= new InMemorySnapShotStorage();
            return _snapshotStorage.StoreSnapshots(streamId, snapshots);
        }
#endregion
    }
}