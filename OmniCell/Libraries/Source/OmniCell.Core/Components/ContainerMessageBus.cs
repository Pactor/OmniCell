#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Components
{
    #region Usings ...

    using System.ComponentModel.Composition;

    #endregion

    /// <summary>
    /// The bus an engine gets from its MEF container. Besides its subscribers, every
    /// exported IHandle&lt;T&gt; for a message's type handles the message.
    /// </summary>
    [Export(typeof(IBus))]
    public class ContainerMessageBus : MessageBus
    {
        /// <summary>
        /// </summary>
        /// <param name="container">
        /// Where the IHandle&lt;T&gt; exports come from.
        /// </param>
        [ImportingConstructor]
        public ContainerMessageBus(IContainer container)
            : base(container.GetAllInstances)
        {
        }
    }
}
