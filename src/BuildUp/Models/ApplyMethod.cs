using System;
using System.Linq;
using BuildUp.Extensions;

namespace BuildUp.Models
{
    public class ApplyMethod : IApplyMethod
    {
        public Type ProjectionType { get; set; }
        public Type EventType { get; set; }
        private bool _isDelete;
        private bool _isPatch;

        public void Patch()
        {
            _isPatch = true;
        }

        public void Delete()
        {
            _isDelete = true;
        }
        public Delegate ApplyDelegate { get; set; }

        public bool IsDelete => _isDelete || EventType.IsDelete();
        public bool IsPatchable => _isPatch || EventType.IsPatch();
        
        public bool RequiresOverwrite => (!IsDelete && !IsPatchable);

    }
}