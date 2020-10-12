using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BuildUp.Extensions;
using BuildUp.Models;

namespace BuildUp
{
    public class BuildUp : IBuildUp, IObserver<IBuildUpEvent>
    {
        private readonly BuildUpConfiguration _config;
        private BuildUp(BuildUpConfiguration config)
        {
            _config = config;
            _eventStreamConnection = _config.Subscribe(this);
        }

        public static BuildUp Initialize(Action<IBuildUpInitialization> initialize)
        {
            var config = new BuildUpConfiguration();
            initialize(config);
            var buildup = new BuildUp(config);
            return buildup;
        }

        public T Project<T>(T existing, params object[] events) where T : new()
        {
            var projectionType = typeof(T);
            
            if (!_config.IsProjectable(projectionType))
            {
                return existing;
            }

            if (events.Any(t => t.GetType().IsDelete()))
            {
                return new T();
            }

            var applyDelegates = _config.GetApplyMethods(projectionType); 
            foreach (var e in events)
            {
                var eventType = e.GetType();
                if (applyDelegates.ContainsKey(eventType))
                {
                    applyDelegates[e.GetType()].ApplyDelegate?.DynamicInvoke(existing, e);
                }
                else if (_config.IsTransformable(eventType))
                {
                    var transform = _config.FindTransform(projectionType, eventType);
                    if (transform != null)
                    {
                        var transformedEvent = transform.DynamicInvoke(e);
                        return Project(existing, transformedEvent);   
                    }
                }
            }
            return existing;
        }

        public T Project<T>(params object[] events) where T : new()
        {
            return Project(new T(), events);
        }

        public IEnumerable<IBuildUpSnapshot> GetSnapshots(params object[] events)
        {
            // Step 1:  Find out what projection types we need to create
            var snapShots = new List<IBuildUpSnapshot>();
            var types = events.SelectMany(t => t.GetType().GetCreateTypes()).ToList();
            var registeredCreates = _config.GetProjections();
            foreach (var registeredCreate in registeredCreates)
            {
                if (registeredCreate != null && registeredCreate.GetCreatedBy() != null)
                {
                    if (events.Select(t => t.GetType()).Any(x => x == registeredCreate.GetCreatedBy()))
                        types.Add(registeredCreate.GetProjectionType());
                }
            }
            if (types.Count == 0)
            {
                return snapShots;
            }
            // Step 2:  Create each projection
            foreach (var t in types)
            {
                var proj = Activator.CreateInstance(t);
                var genericType = typeof(BuildUpSnapshot<>).MakeGenericType(t);
                var method = this.GetType().GetMethods(BindingFlags.Public|BindingFlags.Instance)
                                           .FirstOrDefault(t => t.Name == "Project" && t.GetParameters().Length == 2)?
                                           .MakeGenericMethod(t);
                if (method is { }) proj = method.Invoke(this, new object[] {proj, events});
                var snapShot = Activator.CreateInstance(genericType, new object[] {proj, 1});
                snapShots.Add((IBuildUpSnapshot)snapShot);
            }

            return snapShots;
        }

        public IEnumerable<ApplyMethod> GetApplyMethods() => _config.GetApplyMethods();
        public IEnumerable<BuildUpProjection> GetProjections() => _config.GetProjections();

        private IDisposable _eventStreamConnection;
        
        public void OnCompleted()
        {
            
        }

        public void OnError(Exception error)
        {
            _eventStreamConnection?.Dispose();
        }

        public void OnNext(IBuildUpEvent value)
        {
            Task.Factory.StartNew(async () => await HandleEvent(value));
        }

        private async Task HandleEvent(IBuildUpEvent @event)
        {
            // First.. store the event
            await _config.StoreEvents(@event);
            // Now we need to get snapshots and update
            var ss = await _config.GetSnapshots(@event.StreamId);
            var buildUpSnapshots = ss.ToList();
            if (!buildUpSnapshots.Any())
            {
                // we need to get all events and attempt to aggregate if there are anything to aggregate
                var events = await AccumulateEvents(@event.StreamId);
                ss = GetSnapshots(events.Select(t => t.Data).ToArray());
                var upSnapshots = ss.ToList();
                if (upSnapshots.Any())
                {
                    await _config.StoreSnapshots(@event.StreamId, upSnapshots);
                }
            }
            else
            {
                var updatedSs = new List<IBuildUpSnapshot>(); 
                // Update snapshots
                foreach (var snapShot in buildUpSnapshots)
                {
                    updatedSs.Add(UpdateSnapshot(snapShot, @event));
                }

                await _config.StoreSnapshots(@event.StreamId, updatedSs.ToArray());
            }
        }

        private IBuildUpSnapshot UpdateSnapshot(IBuildUpSnapshot snapShot, IBuildUpEvent @event)
        {
            var method = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(t => t.Name == "Project" && t.GetParameters().Length == 2);
            var generic = method.MakeGenericMethod(snapShot.ProjectionType);
            var type = typeof(BuildUpSnapshot<>).MakeGenericType(snapShot.ProjectionType);
            return (IBuildUpSnapshot)Activator.CreateInstance(type,
                new object[] {generic.Invoke(this, new object[] {snapShot.Snapshot, @event.Data}), @event.Version});
        }

        public const int RETRY_COUNT = 5;
        private async Task<IEnumerable<IBuildUpEvent>> AccumulateEvents(Guid streamId, int retries = 0)
        {
            try
            {
                var events = new List<IBuildUpEvent>();
                
                var page = await _config.GetEventPage(streamId, int.MaxValue, 100);
                events.AddRange(page.Events);

                while (events.Count < page.Total)
                {
                    page = await _config.GetEventPage(streamId, page.MaxVersion, 100);
                    events.AddRange(page.Events);
                }

                return events;
            }
            catch (Exception e)
            {
                if (retries < RETRY_COUNT)
                {
                    return await AccumulateEvents(streamId, retries + 1);
                }

                throw;
            }
        }
    }
}
