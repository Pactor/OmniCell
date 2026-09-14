#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Components
{
    #region Usings ...

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;

    using Utility;

    #endregion

    /// <summary>
    /// Publish/subscribe inside one engine. Replaces MemBus.
    /// </summary>
    /// <remarks>
    /// Delivery is asynchronous and ordered: Publish(message) keeps this bus's own
    /// order, Publish(message, queue) keeps the order of the given queue, so a zone
    /// or login client passes its own and one slow client does not hold up the
    /// others. As with MemBus, a message goes to every subscriber whose type it can
    /// be assigned to. A subscriber that throws is logged and the rest still run.
    /// </remarks>
    public class MessageBus : IBus
    {
        #region Fields

        private readonly ConcurrentDictionary<Type, MethodInfo> handleMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        private readonly Func<Type, IEnumerable<object>> handlerSource;

        private readonly SerialQueue order = new SerialQueue();

        private readonly object subscriptionLock = new object();

        private volatile Subscription[] subscriptions = new Subscription[0];

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// A bus that delivers to its subscribers only.
        /// </summary>
        public MessageBus()
            : this(null)
        {
        }

        /// <summary>
        /// A bus that also delivers to handler objects.
        /// </summary>
        /// <param name="handlerSource">
        /// Given a closed IHandle&lt;T&gt; type, returns the objects that handle that message
        /// type. Asked for every message.
        /// </param>
        public MessageBus(Func<Type, IEnumerable<object>> handlerSource)
        {
            this.handlerSource = handlerSource;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Hands a message to every subscriber and handler now, on the calling thread.
        /// </summary>
        public void Deliver(object message)
        {
            Type messageType = message.GetType();
            foreach (Subscription subscription in this.subscriptions)
            {
                if (!subscription.MessageType.IsAssignableFrom(messageType))
                {
                    continue;
                }

                try
                {
                    subscription.Invoke(message);
                }
                catch (Exception e)
                {
                    LogUtil.ErrorException(
                        e,
                        "Subscriber for {0} threw while handling {1}",
                        subscription.MessageType.Name,
                        messageType.Name);
                }
            }

            if (this.handlerSource != null)
            {
                this.DeliverToHandlers(message, messageType);
            }
        }

        /// <summary>
        /// Queues a message behind everything else published to this bus without a queue.
        /// </summary>
        public void Publish(object message)
        {
            this.Publish(message, this.order);
        }

        /// <summary>
        /// Queues a message behind everything else posted to the given queue.
        /// </summary>
        public void Publish(object message, SerialQueue queue)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            (queue ?? this.order).Post(() => this.Deliver(message));
        }

        /// <summary>
        /// Subscribes to messages of type T and anything derived from it. Dispose the
        /// result to unsubscribe.
        /// </summary>
        public IDisposable Subscribe<T>(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            Subscription subscription = new Subscription(this, typeof(T), message => action((T)message));
            lock (this.subscriptionLock)
            {
                Subscription[] grown = new Subscription[this.subscriptions.Length + 1];
                Array.Copy(this.subscriptions, grown, this.subscriptions.Length);
                grown[grown.Length - 1] = subscription;
                this.subscriptions = grown;
            }

            return subscription;
        }

        #endregion

        #region Methods

        private void DeliverToHandlers(object message, Type messageType)
        {
            Type handlerType = typeof(IHandle<>).MakeGenericType(messageType);
            MethodInfo handle = this.handleMethods.GetOrAdd(handlerType, type => type.GetMethod("Handle"));

            IEnumerable<object> handlers;
            try
            {
                handlers = this.handlerSource(handlerType);
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e, "Could not get the handlers for {0}", messageType.Name);
                return;
            }

            foreach (object handler in handlers)
            {
                try
                {
                    handle.Invoke(handler, new[] { message });
                }
                catch (TargetInvocationException e)
                {
                    LogUtil.ErrorException(
                        e.InnerException ?? e,
                        "{0} threw while handling {1}",
                        handler.GetType().Name,
                        messageType.Name);
                }
            }
        }

        private void Remove(Subscription subscription)
        {
            lock (this.subscriptionLock)
            {
                int index = Array.IndexOf(this.subscriptions, subscription);
                if (index < 0)
                {
                    return;
                }

                Subscription[] shrunk = new Subscription[this.subscriptions.Length - 1];
                Array.Copy(this.subscriptions, 0, shrunk, 0, index);
                Array.Copy(this.subscriptions, index + 1, shrunk, index, shrunk.Length - index);
                this.subscriptions = shrunk;
            }
        }

        #endregion

        private sealed class Subscription : IDisposable
        {
            public readonly Action<object> Invoke;

            public readonly Type MessageType;

            private readonly MessageBus bus;

            public Subscription(MessageBus bus, Type messageType, Action<object> invoke)
            {
                this.bus = bus;
                this.MessageType = messageType;
                this.Invoke = invoke;
            }

            public void Dispose()
            {
                this.bus.Remove(this);
            }
        }
    }
}
