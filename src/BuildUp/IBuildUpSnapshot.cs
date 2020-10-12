using System;

namespace BuildUp
{
    public interface IBuildUpSnapshot
    {
        Type ProjectionType { get; }
        object Snapshot { get; }
        int Version { get; }
    }

    public interface IBuildUpSnapshot<in T> : IBuildUpSnapshot where T : new()
    {
    }
}