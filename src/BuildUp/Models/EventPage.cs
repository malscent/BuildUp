using System.Collections.Generic;

namespace BuildUp.Models
{
    public class EventPage : IEventPage
    {
        public int Count { get; set; }
        public int Total { get; set; }
        public int MaxVersion { get; set; }
        public IEnumerable<IBuildUpEvent> Events { get; set; }
    }
}