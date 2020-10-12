using System.Diagnostics.Tracing;
using System.Linq;
using Xunit;

namespace BuildUp.Test
{
    public class ProjectionRegistrationTests
    {
        public class CreateEvent
        {
            public int Id { get; set; }
        }

        public class ModifyEvent
        {
            public int Id { get; set; }
        }
        
        public class SignatureProjection
        {
            public int Id { get; set; }

            public SignatureProjection NameDoesNotMatter(SignatureProjection proj, CreateEvent e)
            {
                proj.Id = e.Id;
                return proj;
            }
            public SignatureProjection NameDoesNotMatter(SignatureProjection proj, IsCreateSignatureProjectionEvent e)
            {
                proj.Id = e.Id;
                return proj;
            }
            public static SignatureProjection NameDoesNotMatter(SignatureProjection proj, ModifyEvent e)
            {
                proj.Id = e.Id;
                return proj;
            }
        }

        [Fact]
        public void CanRegisterAProjectionAndApplyMethodsAreFoundBySignature()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterProjection<SignatureProjection>();
            });
            var methods = buildUp.GetApplyMethods();
            Assert.Equal(3, methods.Count());
        }
        
        [Fact]
        public void CanApplyMultipleMethodsToProjection()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterProjection<SignatureProjection>();
            });
            var projection = new SignatureProjection();
            var create = new CreateEvent
            {
                Id = 1
            };
            var modify = new ModifyEvent
            {
                Id = 3
            };
            projection = buildUp.Project(projection, create, modify);
            Assert.Equal(3, projection.Id);
        }

        [Fact]
        void CanSpecifyWhatEventTypeCreatesAProjectionOnAStream()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterProjection<SignatureProjection>().CreatedBy<CreateEvent>();
            });
            var @event = new CreateEvent
            {
                Id = 72
            };
            var snapshots = buildUp.GetSnapshots(@event);
            var buildUpSnapshots = snapshots as IBuildUpSnapshot[] ?? snapshots.ToArray();
            Assert.Single(buildUpSnapshots);
            var snapshot = buildUpSnapshots.First();
            Assert.Equal(72, ((SignatureProjection)snapshot.Snapshot).Id);
        }
        [Creates(typeof(SignatureProjection))]
        public class IsCreateSignatureProjectionEvent
        {
            public int Id { get; set; }
        }
        [Fact]
        void CanSpecifyWhatEventTypeCreatesAProjectionFromAttribute()
        {
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterProjection<SignatureProjection>();
            });

            var @event = new IsCreateSignatureProjectionEvent
            {
                Id = 5
            };
            var projections = buildUp.GetSnapshots(@event);
            var buildUpSnapshots = projections as IBuildUpSnapshot[] ?? projections.ToArray();
            Assert.Single(buildUpSnapshots);
            var proj = buildUpSnapshots.First();
            Assert.Equal(5, ((SignatureProjection)proj.Snapshot).Id);
        }
        
    }
}