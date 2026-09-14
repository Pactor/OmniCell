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
    using System.Collections.Generic;
    using System.Threading;

    using Utility;

    #endregion

    /// <summary>
    /// Runs work on the thread pool one item at a time, in the order it was posted.
    /// </summary>
    /// <remarks>
    /// MemBus's asynchronous bus gave every message its own task, so two messages
    /// from one client could be handled in either order and packets went out
    /// jumbled. One queue per client, or per playfield, keeps that order while
    /// different queues still run in parallel. An empty queue holds no thread.
    /// </remarks>
    public sealed class SerialQueue
    {
        private readonly Queue<Action> work = new Queue<Action>();

        private bool draining;

        /// <summary>
        /// Queues an action behind everything posted before it.
        /// </summary>
        public void Post(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            lock (this.work)
            {
                this.work.Enqueue(action);
                if (this.draining)
                {
                    return;
                }

                this.draining = true;
            }

            ThreadPool.QueueUserWorkItem(Drain, this);
        }

        private static void Drain(object state)
        {
            SerialQueue queue = (SerialQueue)state;
            while (true)
            {
                Action action;
                lock (queue.work)
                {
                    if (queue.work.Count == 0)
                    {
                        queue.draining = false;
                        return;
                    }

                    action = queue.work.Dequeue();
                }

                try
                {
                    action();
                }
                catch (Exception e)
                {
                    LogUtil.ErrorException(e, "A queued bus message threw");
                }
            }
        }
    }
}
