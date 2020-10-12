using System;

namespace BuildUp.Models
{
    public class Transform
    {
        public Type OldType { get; set; }
        public Type NewType { get; set; }
        public Delegate TransformDelegate { get; set; }
    }
}