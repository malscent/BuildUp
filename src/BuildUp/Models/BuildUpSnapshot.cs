using System;

namespace BuildUp.Models
{
    public class BuildUpSnapshot<T> : IBuildUpSnapshot<T> where T: new()
    {
        public Type ProjectionType => typeof(T);
        private readonly T _data;
        public int Version { get; private set; }

        public BuildUpSnapshot(T data, int version)
        {
            _data = data;
            Version = version;
        }
        public object Snapshot => _data;
        
    }
}