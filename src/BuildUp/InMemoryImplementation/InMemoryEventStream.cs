using System;
using System.Collections.Generic;

namespace BuildUp.InMemoryImplementation
{
    public class InMemoryEventStream : IEventStream
    {
        public List<IObserver<IBuildUpEvent>> _observers = new List<IObserver<IBuildUpEvent>>();
        
        public IDisposable Subscribe(IObserver<IBuildUpEvent> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber(_observers, observer);
        }

        public void PublishEvent(IBuildUpEvent @event)
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(@event);
            }
        }

        public void BeginStream()
        {
            // In a real implementation this would start listening to whatever source
        }

        public void EndStream()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
            _observers.Clear();
        }
        
        private class Unsubscriber : IDisposable
        {
            private List<IObserver<IBuildUpEvent>> _observers;
            private IObserver<IBuildUpEvent> _observer;

            public Unsubscriber(List<IObserver<IBuildUpEvent>> observers, IObserver<IBuildUpEvent> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                {
                    _observers.Remove(_observer);
                }
            }
        }
    }
}