using System;
using System.Collections.Generic;

namespace BuildUp.Models
{
    public class BuildUpProjection : IBuildUpProjection
    {
        private Type _createdBy;
        private Type _projectionType;
        private IEnumerable<ApplyMethod> _applyMethods; 

        public BuildUpProjection(Type projectionType, IEnumerable<ApplyMethod> applyMethods)
        {
            _projectionType = projectionType;
            _applyMethods = applyMethods;
        }
        
        public void CreatedBy<T>()
        {
            _createdBy = typeof(T);
        }

        internal Type GetCreatedBy() => _createdBy;
        internal Type GetProjectionType() => _projectionType;
    }
}