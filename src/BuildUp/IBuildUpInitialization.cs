using System;
using System.Diagnostics;
using BuildUp.Models;

namespace BuildUp
{
    public interface IBuildUpInitialization
    {
        IApplyMethod RegisterApply<TProjection, TEvent>(Func<TProjection, TEvent, TProjection> apply);
        void RegisterEventTransform<TOld, TNew>(Func<TOld, TNew> transform);
        IBuildUpProjection RegisterProjection<T>() where T : new();

        void SetEventStream(IEventStream stream);
        void SetEventStorage(IEventStorage store);
        void SetEventProvider(IEventProvider provider);
        void SetSnapshotStorage(ISnapshotStorage store);
        void SetSnapshotProvider(ISnapshotProvider provider);
    }
}