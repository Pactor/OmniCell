#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace OmniCell.Core.Components
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    using SmokeLounge.AOtomation.Messaging.Messages;

    using Utility;

    #endregion

    /// <summary>
    /// </summary>
    [Export(typeof(IMessagePublisher))]
    public class MessagePublisher : IMessagePublisher
    {
        #region Fields

        /// <summary>
        /// </summary>
        private readonly Dictionary<Type, IList<IHandleMessage>> messageHandlers;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        /// <param name="messageHandlers">
        /// </param>
        [ImportingConstructor]
        public MessagePublisher([ImportMany] IEnumerable<IHandleMessage> messageHandlers)
        {
            this.messageHandlers = new Dictionary<Type, IList<IHandleMessage>>();

            foreach (IHandleMessage messageHandler in messageHandlers)
            {
                Type handlerInterface =
                    messageHandler.GetType()
                        .GetInterfaces()
                        .FirstOrDefault(i => typeof(IHandleMessage).IsAssignableFrom(i) && i.IsGenericType);
                if (handlerInterface == null)
                {
                    continue;
                }

                Type arg = handlerInterface.GetGenericArguments().FirstOrDefault();
                if (arg == null)
                {
                    continue;
                }

                IList<IHandleMessage> handlers;
                if (this.messageHandlers.TryGetValue(arg, out handlers) == false)
                {
                    handlers = new List<IHandleMessage>();
                    this.messageHandlers.Add(arg, handlers);
                }

                handlers.Add(messageHandler);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="message">
        /// </param>
        public void Publish(object sender, Message message)
        {
            Type messageType = message.Body.GetType();

            IList<IHandleMessage> handlers;
            if (this.messageHandlers.TryGetValue(messageType, out handlers) == false)
            {
                // Most of the client's message types have no handler yet. Returning in
                // silence makes "not implemented" look exactly like "broken", so say so.
                LogUtil.Debug(
                    DebugInfoDetail.NetworkMessages,
                    "No handler registered for message " + messageType.Name);
                return;
            }

            foreach (IHandleMessage handler in handlers)
            {
                try
                {
                    handler.Handle(sender, message);
                }
                catch (Exception exception)
                {
                    // The bus dispatches handlers asynchronously and discards anything
                    // they throw, so a failure in here was previously invisible: no log
                    // line, and the client simply never received its reply. Report it and
                    // continue with the remaining handlers for this message.
                    LogUtil.ErrorException(
                        exception,
                        "Handler {0} threw while handling {1}",
                        handler.GetType().Name,
                        messageType.Name);
                }
            }
        }

        #endregion
    }
}