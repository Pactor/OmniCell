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

namespace OmniCell.Communication.ISComV2Server
{
    #region Usings ...

    using System;

    using Cell.Core;

    using OmniCell.Communication.Messages;

    using MsgPack.Serialization;

    using Utility;

    #endregion

    /// <summary>
    /// </summary>
    public class ISComV2ClientHandler : ClientBase
    {
        #region Fields

        /// <summary>
        /// </summary>
        private readonly int ID;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        /// <param name="server">
        /// </param>
        public ISComV2ClientHandler(ServerBase server)
            : base(server)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="server">
        /// </param>
        /// <param name="clientNumber">
        /// </param>
        public ISComV2ClientHandler(ServerBase server, int clientNumber)
            : base(server)
        {
            this.ID = clientNumber;
        }

        #endregion

        #region Delegates

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="dataBytes">
        /// </param>
        public delegate void DataReceivedHandler(object sender, OnDataReceivedArgs e);

        #endregion

        #region Public Events

        /// <summary>
        /// </summary>
        public event DataReceivedHandler DataReceived;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public int GetID()
        {
            return this.ID;
        }

        /// <summary>
        /// </summary>
        /// <param name="dataObject">
        /// </param>
        public void Send(DynamicMessage dataObject)
        {
            MessagePackSerializer<object> serializer = MessagePackSerializer.Create<object>();
            byte[] data = serializer.PackSingleObject(dataObject);
            byte[] header = new byte[8];
            BitConverter.GetBytes(0x00ff55aa).CopyTo(header, 0);
            BitConverter.GetBytes(data.Length).CopyTo(header, 4);
            this.Send(header);
            this.Send(data);
        }

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        public void Send(MessageBase message)
        {
            var temp = new DynamicMessage();
            temp.DataObject = message;
            this.Send(temp);
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="buffer">
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        protected override bool OnReceive(BufferSegment buffer)
        {
            // return false, if header cannot be complete (00FF55AA, <Length of packet>)

            // Loop if more than one packet frame received
            while (true)
            {
                if (this._remainingLength == 0)
                {
                    return true;
                }

                if (this._remainingLength < HeaderLength)
                {
                    return false;
                }

                int expectedLength = this.CheckData(buffer);
                if (expectedLength == -1)
                {
                    // MALFORMED PACKET RECEIVED !!!
                    LogUtil.Debug(DebugInfoDetail.Error, "Malformed packet received: ");
                    byte[] data = new byte[this._remainingLength];
                    Array.Copy(buffer.SegmentData, this._offset, data, 0, this._remainingLength);
                    LogUtil.Debug(DebugInfoDetail.Error, HexOutput.Output(data));
                    this._remainingLength = 0;
                    this._offset = 0;

                    // Lets clear the buffer and try this again, no need to drop the connection
                    return true;
                }

                if (expectedLength + HeaderLength > this._remainingLength)
                {
                    return false;
                }

                if (this._remainingLength >= expectedLength + HeaderLength)
                {
                    // Handle packet payload here

                    byte[] dataBytes = new byte[expectedLength];
                    Array.Copy(buffer.SegmentData, HeaderLength + this._offset, dataBytes, 0, expectedLength);
                    if (this.DataReceived != null)
                    {
                        this.DataReceived(this, new OnDataReceivedArgs() { dataBytes = dataBytes });
                    }
                    else
                    {
                        LogUtil.Debug(DebugInfoDetail.Error, "No DataReceived event fired due to missing method");
                    }
                }

                if (expectedLength + HeaderLength <= this._remainingLength)
                {
                    // If we have received a full packet frame
                    // then move the remaining data to a new buffer (with offset 0)
                    // only adjusting offset and length here
                    // Then do the whole thing again
                    this._remainingLength -= expectedLength + HeaderLength;
                    this._offset += expectedLength + HeaderLength;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="buffer">
        /// </param>
        /// <returns>
        /// </returns>
        /// <summary>
        /// Framing before the payload: magic (4 bytes) + length (4 bytes).
        /// </summary>
        private const int HeaderLength = 8;

        /// <summary>
        /// Largest payload a single frame may declare. The length field is read straight
        /// off the wire, so it is bounded here before it reaches an allocation.
        /// Unbounded, it slipped past both guards below in two ways: a negative length
        /// makes "length + HeaderLength > remaining" false and "remaining >= length +
        /// HeaderLength" true, reaching new byte[negative]; and a length within
        /// int.MaxValue-7..int.MaxValue wraps the same arithmetic negative for the same
        /// effect. A frame larger than the receive buffer could never be reassembled.
        /// </summary>
        private const int MaxFrameLength = ClientBase.BufferSize - HeaderLength;

        private int CheckData(BufferSegment buffer)
        {
            if (BitConverter.ToInt32(buffer.SegmentData, this._offset) != 0x00FF55AA)
            {
                LogUtil.Debug(DebugInfoDetail.Error, "Invalid packet header");
                return -1;
            }

            int length = BitConverter.ToInt32(buffer.SegmentData, this._offset + 4);

            if (length < 0 || length > MaxFrameLength)
            {
                LogUtil.Debug(DebugInfoDetail.Error, "Invalid packet length");
                return -1;
            }

            return length;
        }

        #endregion
    }
}