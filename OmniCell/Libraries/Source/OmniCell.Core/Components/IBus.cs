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

    #endregion

    /// <summary>
    /// </summary>
    public interface IBus
    {
        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        void Publish(object message);

        /// <summary>
        /// Hands a message to its subscribers and handlers now, on the calling thread.
        /// </summary>
        /// <param name="message">
        /// </param>
        void Deliver(object message);

        /// <summary>
        /// Publishes a message in the order of the given queue rather than the bus's own.
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <param name="queue">
        /// One per client, so each client's messages are handled in the order they arrived.
        /// </param>
        void Publish(object message, SerialQueue queue);

        /// <summary>
        /// </summary>
        /// <param name="action">
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        IDisposable Subscribe<T>(Action<T> action);

        #endregion
    }
}