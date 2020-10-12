using System;

namespace BuildUp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class CreatesAttribute : Attribute
    {
        public CreatesAttribute(Type projectionType)
        {
            Creates = projectionType;
        }

        public Type Creates { get; }
    }
}