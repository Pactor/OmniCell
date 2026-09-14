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

namespace ChatEngine.PacketHandlers
{
    #region Usings ...

    using ChatEngine.CoreClient;
    using ChatEngine.Packets;

    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;

    #endregion

    /// <summary>
    /// The buddy add.
    /// </summary>
    public static class BuddyAdd
    {
        #region Public Methods and Operators

        /// <summary>
        /// Read buddy add packet
        /// </summary>
        /// <param name="client">
        /// Client sending
        /// </param>
        /// <param name="packet">
        /// Packet data
        /// </param>
        public static void Read(Client client, byte[] packet)
        {
            PacketReader reader = new PacketReader(ref packet);

            reader.ReadUInt16(); // Packet ID
            reader.ReadUInt16(); // Data length
            uint playerId = reader.ReadUInt32();
            ushort unknown1 = reader.ReadUInt16();
            byte unknown2 = reader.ReadByte();
            client.Server.Debug(
                client,
                "{0} >> BuddyAdd: PlayerID {1} Unknown1: {2} Unknown2: {3}",
                client.Character.characterName,
                playerId,
                unknown1,
                unknown2);
            reader.Finish();

            // Remembered, so this connection is told when the buddy logs on or
            // off (ChatServer.NotifyBuddies).
            lock (client.Buddies)
            {
                client.Buddies.Add(playerId);
            }

            // The buddy's status, as the live chat server answers a buddy add:
            // in the retail chat captures each client buddy add is followed by a
            // packet 40 with the id, online 0 or 1, and status bytes 00 01 00.
            // Nothing was sent, so a bot never learned whether a buddy was on.
            // Online means connected to chat, the same test the logon and logoff
            // notifications use; the database flag is only set for bot logins.
            bool online;
            lock (client.ChatServer().ConnectedClients)
            {
                online = client.ChatServer().ConnectedClients.ContainsKey(playerId);
            }

            client.Send(BuddyOnlineStatus.Create(playerId, online ? 1u : 0u, new byte[] { 0x00, 0x01, 0x00 }));
        }

        #endregion
    }
}