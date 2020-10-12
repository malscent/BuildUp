using System;
using System.Runtime;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace BuildUp.Test
{
    public class BasicOperations
    {
        public class ExampleProjection
        {
            public Guid Id { get; set; }
            public string SomeValue { get; set; }
        }

        public class ExampleEvent
        {
            public Guid Id { get; set; }
            public string AppendValue { get; set; }
        }

        private readonly Func<ExampleProjection, ExampleEvent, ExampleProjection> _apply = (projection, @event) =>
        {
            projection.Id = @event.Id;
            projection.SomeValue += @event.AppendValue;
            return projection;
        };

        [Fact]
        public void CanRegisterAnApplyMethod()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_apply);
            });
        }

        public class ObsoleteEvent
        {
            public Guid Id { get; set; }
            public string NewValue { get; set; }
        }

        private Func<ObsoleteEvent, ExampleEvent> _transform = @event => new ExampleEvent
        {
            Id = @event.Id,
            AppendValue = @event.NewValue
        }; 
        [Fact]
        public void CanRegisterAnEventTransform()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterEventTransform(_transform);
            });
        }

        [Fact]
        public void CanApplyAnEventWithNoTransform()
        {
            var existing = new ExampleProjection();
            var @event = new ExampleEvent
            {
                Id = Guid.NewGuid(),
                AppendValue = "AppendingValue"
            };
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_apply);
            });
            var projectedValue = buildUp.Project(existing, @event);
            
            Assert.Equal(projectedValue.Id, @event.Id);
            Assert.Equal(projectedValue.SomeValue, @event.AppendValue);
        }

        [Fact]
        public void CanApplyMultipleEvents()
        {
            var existing = new ExampleProjection();
            var @event = new ExampleEvent
            {
                Id = Guid.NewGuid(),
                AppendValue = "AppendingValue"
            };
            var secondEvent = new ExampleEvent
            {
                Id = @event.Id,
                AppendValue = "MoreValuesToAdd"
            };
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_apply);
            });
            var projectedValue = buildUp.Project(existing, @event, secondEvent);
            
            Assert.Equal(projectedValue.Id, @event.Id);
            Assert.Equal(projectedValue.SomeValue, $"{@event.AppendValue}{secondEvent.AppendValue}");
        }

        [Fact]
        public void AutomaticallyPerformsTransformIfPresent()
        {
            var existing = new ExampleProjection();
            var @event = new ExampleEvent
            {
                Id = Guid.NewGuid(),
                AppendValue = "AppendingValue"
            };
            var secondEvent = new ObsoleteEvent()
            {
                Id = @event.Id,
                NewValue = "MoreValuesToAdd"
            };
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_apply);
                _.RegisterEventTransform(_transform);
            });
            var projectedValue = buildUp.Project(existing, @event, secondEvent);
            
            Assert.Equal(projectedValue.Id, @event.Id);
            Assert.Equal(projectedValue.SomeValue, $"{@event.AppendValue}{secondEvent.NewValue}");
        }

        public class ChainedEvent
        {
            public Guid Id { get; set; }
            public string OldValue { get; set; }
        }
        private Func<ChainedEvent, ObsoleteEvent> _chainedTransform = @event => new ObsoleteEvent()
        {
            Id = @event.Id,
            NewValue = @event.OldValue
        };
        
        [Fact]
        public void ChainsTransformsToFindAWayToApply()
        {
            var existing = new ExampleProjection();
            var @event = new ExampleEvent
            {
                Id = Guid.NewGuid(),
                AppendValue = "AppendingValue"
            };
            var secondEvent = new ChainedEvent()
            {
                Id = @event.Id,
                OldValue = "MoreValuesToAdd"
            };
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_apply);
                _.RegisterEventTransform(_transform);
                _.RegisterEventTransform(_chainedTransform);
            });
            var projectedValue = buildUp.Project(existing, @event, secondEvent);
                
            Assert.Equal(projectedValue.Id, @event.Id);
            Assert.Equal(projectedValue.SomeValue, $"{@event.AppendValue}{secondEvent.OldValue}");
        }
    }
}
