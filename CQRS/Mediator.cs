using CQRS.Commands;
using CQRS.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CQRS
{
    public class Mediator : ICommandSender, IEventPublisher
    {
        private readonly Dictionary<Type, List<Func<Message, Task>>> _handlers = new Dictionary<Type, List<Func<Message, Task>>>();

        public void RegisterHandler<T>(Func<T, Task> handler) where T : Message
        {
            List<Func<Message, Task>> handlers;

            if (!_handlers.TryGetValue(typeof(T), out handlers))
            {
                handlers = new List<Func<Message, Task>>();
                _handlers.Add(typeof(T), handlers);
            }

            handlers.Add((x => handler((T)x)));
        }

        public async Task Send<T>(T command) where T : Command
        {
            List<Func<Message, Task>> handlers;

            if (_handlers.TryGetValue(typeof(T), out handlers))
            {
                if (handlers.Count != 1) throw new InvalidOperationException("cannot send to more than one handler");
                await handlers[0](command);
            }
            else
            {
                throw new InvalidOperationException("no handler registered");
            }
        }

        public async Task Publish<T>(T @event) where T : Event
        {
            List<Func<Message, Task>> handlers;

            if (!_handlers.TryGetValue(@event.GetType(), out handlers)) return;

            foreach (var handler in handlers)
            {
                //dispatch on thread pool for added awesomeness
                var handler1 = handler;
                ThreadPool.QueueUserWorkItem(async x => await handler1(@event));
            }
        }

        public void ClearHandlers()
        {
            _handlers.Clear();
        }
    }

    public interface Handles<T>
    {
        Task Handle(T message);
    }

    public interface ProjectionHandles<T>
    {
        void Handle(T message);
        string GetKeyFromMessage(T message);
    }

    public interface ICommandSender
    {
        Task Send<T>(T command) where T : Command;

    }
    public interface IEventPublisher
    {
        Task Publish<T>(T @event) where T : Event;
    }
}
