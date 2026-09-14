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

    #endregion

    /// <summary>
    /// Collects bus subscriptions so they can be disposed together.
    /// </summary>
    public sealed class DisposeContainer : IDisposable
    {
        private readonly List<IDisposable> items = new List<IDisposable>();

        /// <summary>
        /// Adds something to dispose with the rest.
        /// </summary>
        public void Add(IDisposable item)
        {
            lock (this.items)
            {
                this.items.Add(item);
            }
        }

        /// <summary>
        /// Disposes everything added so far.
        /// </summary>
        public void Dispose()
        {
            IDisposable[] toDispose;
            lock (this.items)
            {
                toDispose = this.items.ToArray();
                this.items.Clear();
            }

            foreach (IDisposable item in toDispose)
            {
                item.Dispose();
            }
        }
    }
}
