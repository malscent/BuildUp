using System;
using System.Runtime.InteropServices;

namespace BuildUp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DeleteAttribute : Attribute
    {
        
    }
}