using System;
using System.Collections.Generic;
using BuildUp.Models;

namespace BuildUp
{
    public interface IBuildUp
    {
        public T Project<T>(T existing, params object[] @events) where T : new();
        public T Project<T>(params object[] @events) where T : new();
        
        public IEnumerable<IBuildUpSnapshot> GetSnapshots(params object[] events);

        public IEnumerable<ApplyMethod> GetApplyMethods();
        public IEnumerable<BuildUpProjection> GetProjections();

    }
}