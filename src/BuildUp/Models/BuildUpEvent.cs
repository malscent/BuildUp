using System;

namespace BuildUp.Models
{
    public class BuildUpEvent<T> : IBuildUpEvent
    {
        public Guid StreamId { get; set; }
        public Type EventType => typeof(T);
        public object Data { get; set; }
        public DateTime EventDate { get; set; } = DateTime.UtcNow;
        public int Version { get; set; }
    }
}