# BuildUp - EventSourcing Helper

BuildUp is a simple library for managing the building of projections from a stream of events.  It currently only has in-memory implementations, but has clear abstractions for implementing key components of an event sourced application.  Not all of these components are necessary for every event sourced application.  Some are null operators such as if storage is not used.  IE.. See NullStorage class in InMemoryImplementation.


## Key Components

* EventStream
* EventStorage
* EventRetrieval
* SnapshotStorage
* SnapshotRetrieval
* SnapshotBuilder

### Event Stream

The event stream is the source of the events.  This could be a Kafka topic, a RabbitMQ Queue, or an in memory stream if self publishing.

### IEventStorage

Event storage is long term storage of events.

### IEventProvider

Event retrieval is the way Buildup will request events by stream to build snapshots.

### ISnapshotStorage

Snapshot storage is long term storage of the BuildUpSnapshots

### ISnapshotProvider

Snapshot retrieval is the way BuildUp will request snapshots to apply events for update.

### Snapshot Builder

Buildup acts as the Snapshot Builder.  It takes a stream's events and identifies what projections to create and builds and stores them using the ISnapshotStorage.  

## Decoupling

These interfaces are decoupled because not every application needs to be concerned about each aspect of an event sourcing application.  

## Usage

Using buildup, you need to define which of the key elements are being used.  You also need to add logic for how to build projections. 

### Defining Key Components

Buildup provides a method called Initialize that accepts an Action<IBuildupInitialization> to configure/setup Buildup. 
To define the different key components, you can do the following:

```c#
            var es = new InMemoryEventStream();
            var eStorage = new InMemoryEventStorage();
            var sStorage = new InMemorySnapShotStorage();
            var buildup = BuildUp.Initialize(t =>
            {
                t.SetEventStream(es);
                t.SetEventProvider(eStorage);
                t.SetEventStorage(eStorage);
                t.SetSnapshotProvider(sStorage);
                t.SetSnapshotStorage(sStorage);
            });
```

### Registering Projections

When you define projections, you must provide some information to Buildup to handle building that projection.  You can do individual Apply Method delegates or you can have ApplyMethods with the T Apply(T proj, X event) on the projection itself.

*Registering an Apply Method*
```c#
        private readonly Func<ExampleProjection, ExampleEvent, ExampleProjection> _apply = (projection, @event) =>
        {
            projection.Id = @event.Id;
            projection.SomeValue += @event.AppendValue;
            return projection;
        };
        var buildUp = BuildUp.Initialize(_ =>
        {
            _.RegisterApply(_apply);
        });
```

*Projection Registration with Apply*
```c#
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

        var buildUp = BuildUp.Initialize(_ =>
        {
            _.RegisterProjection<SignatureProjection>();
        });
```

## Multiple Projections Per Stream

Multiple projections per stream is a common pattern in Event Sourced applications.  To handle this, Buildup utilizes both explicit projection creation during registration and also by event attributes

*Defining Create Events At Registration*
```c#
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

        public class CreateEvent
        {
            public int Id { get; set; }
        }

        var buildUp = BuildUp.Initialize(_ =>
        {
            _.RegisterProjection<SignatureProjection>().CreatedBy<CreateEvent>();
        });
```

*Defining Create events with Attribute*
```c#
        [Creates(typeof(SignatureProjection))]
        public class IsCreateSignatureProjectionEvent
        {
            public int Id { get; set; }
        }

        var buildUp = BuildUp.Initialize(_ =>
        {
            _.RegisterProjection<SignatureProjection>();
        });
```

## Patch and Delete Heuristics

When creating projections, it may be more efficient if we know a delete event is part of the stream of events.  This will allow Buildup to optimize the Snapshot building process by applying patches to Snapshots or by deleting the snapshots altogether.  You can define these operations when registering an apply method by signifying Delete() or Patch() to the apply method or via event attributes.

*Delete() and Patch() method usage*
```c#
            var buildUp = BuildUp.Initialize(_ =>
            {
                _.RegisterApply(_deleteApply).Delete();
                _.RegisterApply(_patchApply).Patch();
            });
```

*Event Attribute Delete/Patch*
```c#
        [Delete]
        public class AlsoDeleteEvent
        {
            
        }

        [Patch]
        public class AlsoPatchEvent
        {
            
        }
```

## TODO List

- [ ] Add Dependency Injection Support
- [ ] Add more testing
- [ ] Implement Kafka Topic Streaming
- [ ] Implement CosmosDB EventStore
- [ ] Implement CosmosDB SnapshotStore
- [ ] Implement MartenDb as EventStore and SnapshotStore
- [ ] Implement CosmosDB EventStreaming with Null Event Storage
- [ ] Create WebAPI Example with InMemoryEventStreaming
