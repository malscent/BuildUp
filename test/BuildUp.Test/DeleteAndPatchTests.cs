using System;
using System.Linq;
using Xunit;

namespace BuildUp.Test
{
    public class DeleteAndPatchTests
    {
        public class DeleteEvent
        {
            
        }

        public class DeleteProjection
        {
            
        }

        public class PatchEvent
        {
            
        }
        private readonly Func<DeleteProjection, DeleteEvent, DeleteProjection> _deleteApply = (projection, @event) =>
        {
            return projection;
        };
        
        private readonly Func<DeleteProjection, PatchEvent, DeleteProjection> _patchApply = (projection, @event) =>
        {
            return projection;
        };
        [Fact]
        public void ApplyCanBeSetAsADeleteViaRegistration()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_deleteApply).Delete();
            });
            var methods = buildUp.GetApplyMethods();
            var method = methods.FirstOrDefault(t => t.EventType == typeof(DeleteEvent) && t.IsDelete);
            Assert.NotNull(method);
        }

        [Fact]
        public void ApplyCanBeSetAsPatchViaRegistration()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_patchApply).Patch();
            });
            var methods = buildUp.GetApplyMethods();
            var method = methods.FirstOrDefault(t => t.EventType == typeof(PatchEvent) && t.IsPatchable);
            Assert.NotNull(method);
        }
        [Delete]
        public class AlsoDeleteEvent
        {
            
        }

        [Patch]
        public class AlsoPatchEvent
        {
            
        }
        private readonly Func<DeleteProjection, AlsoDeleteEvent, DeleteProjection> _alsoDeleteApply = (projection, @event) =>
        {
            return projection;
        };
        
        private readonly Func<DeleteProjection, AlsoPatchEvent, DeleteProjection> _alsoPatchApply = (projection, @event) =>
        {
            return projection;
        };

        [Fact]
        public void CanUseAttributesToSpecifyDeleteEvents()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_alsoDeleteApply);
            });
            var methods = buildUp.GetApplyMethods();
            var method = methods.FirstOrDefault(t => t.EventType == typeof(AlsoDeleteEvent) && t.IsDelete);
            Assert.NotNull(method);
        }
        
        [Fact]
        public void CanUseAttributesToSpecifyPatchEvents()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_alsoPatchApply);
            });
            var methods = buildUp.GetApplyMethods();
            var method = methods.FirstOrDefault(t => t.EventType == typeof(AlsoPatchEvent) && t.IsPatchable);
            Assert.NotNull(method);
        }
    }
}