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

namespace ChatEngine.CoreServer
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    using Cell.Core;

    using OmniCell.Communication.Messages;
    using OmniCell.Database.Dao;

    using ChatEngine.Channels;
    using ChatEngine.CoreClient;
    using ChatEngine.Packets;

    using Chatengine.Relay;

    using ChatEngine.Relay.Common;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;
    using Utility.Config;

    #endregion

    /// <summary>
    /// The server.
    /// </summary>
    public class ChatServer : ServerBase
    {
        #region Fields

        /// <summary>
        /// </summary>
        public HashSet<ChannelBase> Channels = new HashSet<ChannelBase>();

        /// <summary>
        /// </summary>
        public Dictionary<uint, Client> ConnectedClients = new Dictionary<uint, Client>();

        /// <summary>
        /// </summary>
        public string MessageOfTheDay = string.Empty;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        public ChatServer()
        {
            // Global channel
            this.Channels.Add(new GlobalChannel(ChannelFlags.None, ChannelType.General, 1, "Global"));

            // Shopping channels (at the moment just level restricted, no sides)
            this.Channels.Add(new LevelRestrictedChannel(1, 1, 50));
            this.Channels.Add(new LevelRestrictedChannel(2, 51, 150));
            this.Channels.Add(new LevelRestrictedChannel(3, 151, 220));

            // Restricted channels (GM, sided channels)
            this.Channels.Add(new RestrictedChannel(Side.Gm, ChannelFlags.None, ChannelType.GM));
            this.Channels.Add(new RestrictedChannel(Side.Clan, ChannelFlags.None, ChannelType.General));
            this.Channels.Add(new RestrictedChannel(Side.Omni, ChannelFlags.None, ChannelType.General));
            this.Channels.Add(new RestrictedChannel(Side.Neutral, ChannelFlags.None, ChannelType.General));

            // Add a relay channel if needed
            if (ConfigReadWrite.Instance.CurrentConfig.UseIRCRelay)
            {
                this.Channels.Add(
                    new GlobalChannel(
                        ChannelFlags.None,
                        ChannelType.General,
                        5,
                        ConfigReadWrite.Instance.CurrentConfig.RelayIngameChannel));
            }

            this.ClientConnected += this.ClientConnectedToChat;
            this.ClientDisconnected += this.OnClientDisconnect;

            // server welcome message
            this.MessageOfTheDay = ConfigReadWrite.Instance.CurrentConfig.Motd;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public List<ChannelBase> ChannelsByType<T>()
        {
            return this.Channels.Where(x => x is T).ToList();
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="forced">
        /// </param>
        public void OnClientDisconnect(IClient client, bool forced)
        {
            Client cl = (Client)client;
            foreach (ChannelBase channel in cl.Channels.ToArray())
            {
                channel.RemoveClient(cl);
            }

            if (cl.Character.CharacterId != 0)
            {
                CharacterDao.Instance.SetOffline((int)cl.Character.CharacterId);
                bool wasConnected;
                lock (this.ConnectedClients)
                {
                    wasConnected = this.ConnectedClients.TryGetValue(cl.Character.CharacterId, out Client registered)
                                   && registered == cl
                                   && this.ConnectedClients.Remove(cl.Character.CharacterId);
                }

                if (wasConnected)
                {
                    this.NotifyBuddies(cl.Character.CharacterId, false);
                }
            }
        }

        /// <summary>
        /// Tells every connection that has this character as a buddy that it has
        /// logged on or off.
        /// </summary>
        /// <remarks>
        /// The same packet 40 a buddy add is answered with: id, online 0 or 1,
        /// status bytes 00 01 00. In the retail chat captures some arrive with
        /// nothing from the client before them (6 of 13), which is this.
        /// Online here means connected to chat, as for the buddy add answer.
        /// </remarks>
        public void NotifyBuddies(uint characterId, bool online)
        {
            Client[] clients;
            lock (this.ConnectedClients)
            {
                clients = this.ConnectedClients.Values.ToArray();
            }

            byte[] status = BuddyOnlineStatus.Create(characterId, online ? 1u : 0u, new byte[] { 0x00, 0x01, 0x00 });
            foreach (Client other in clients)
            {
                bool isBuddy;
                lock (other.Buddies)
                {
                    isBuddy = other.Buddies.Contains(characterId);
                }

                if (isBuddy && other.Character.CharacterId != characterId)
                {
                    other.Send(status);
                }
            }
        }

        /// <summary>
        /// Registers a character's chat connection and tells its buddies it is on.
        /// Does nothing for a character that is already registered.
        /// </summary>
        public void AddConnectedClient(Client client)
        {
            bool added = false;
            lock (this.ConnectedClients)
            {
                if (!this.ConnectedClients.ContainsKey(client.Character.CharacterId))
                {
                    this.ConnectedClients.Add(client.Character.CharacterId, client);
                    added = true;
                }
            }

            if (added)
            {
                this.NotifyBuddies(client.Character.CharacterId, true);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        internal void AddClientToChannels(Client client)
        {
            // Automatically add client to its appropriate channels
            foreach (ChannelBase channel in this.ChannelsByType<GlobalChannel>())
            {
                channel.AddClient(client);
            }

            foreach (ChannelBase channel in this.ChannelsByType<RestrictedChannel>())
            {
                channel.AddClient(client);
            }

            foreach (ChannelBase channel in this.ChannelsByType<LevelRestrictedChannel>())
            {
                channel.AddClient(client);
            }

            foreach (ChannelBase channel in this.ChannelsByType<TeamChannel>())
            {
                channel.AddClient(client);
            }

            int organizationId = client.Character.orgId;
            if (organizationId != 0)
            {
                OrganizationChannel organizationChannel = this.GetOrCreateOrganizationChannel(organizationId);
                if (organizationChannel != null)
                {
                    organizationChannel.AddClient(client);
                }
            }
        }

        /// <summary>
        /// Gets the chat channel for an existing organization, creating it on the
        /// first member login. Organization channels are never shared between
        /// organizations.
        /// </summary>
        /// <param name="organizationId">Organization id stored in stat 5.</param>
        /// <returns>The matching channel, or null when the organization no longer exists.</returns>
        private OrganizationChannel GetOrCreateOrganizationChannel(int organizationId)
        {
            OrganizationChannel existing = this.ChannelsByType<OrganizationChannel>()
                .OfType<OrganizationChannel>()
                .FirstOrDefault(channel => channel.ChannelId == (uint)organizationId);
            if (existing != null)
            {
                return existing;
            }

            if (OrganizationDao.Instance.Get(organizationId) == null)
            {
                return null;
            }

            var organizationChannel = new OrganizationChannel(organizationId);
            this.Channels.Add(organizationChannel);
            return organizationChannel;
        }

        /// <summary>
        /// </summary>
        /// <param name="packet">
        /// </param>
        /// <returns>
        /// </returns>
        internal ChannelBase GetChannel(byte[] packet)
        {
            byte channelType = packet[4];
            uint chanid = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(packet, 5));

            foreach (ChannelBase ce in this.Channels)
            {
                if ((ce.ChannelId == chanid) && ((byte)ce.channelType == channelType))
                {
                    return ce;
                }
            }

            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="messageObject">
        /// </param>
        internal void ISComDataReceived(object sender, DynamicMessage messageObject)
        {
            var message = messageObject.DataObject as VicinityChatMessage;
            if (message != null)
            {
                this.DistributeVicinityChat(message);
            }
            var requestPlayfieldList = messageObject.DataObject as RequestPlayfieldList;
            if (requestPlayfieldList != null)
            {
                this.PushRequestPlayfieldListReply(requestPlayfieldList);
            }
        }

        private void PushRequestPlayfieldListReply(RequestPlayfieldList requestPlayfieldList)
        {
            lock (Program.Ircbot.replyQueuePlayfieldList)
            {
                LogUtil.Debug(DebugInfoDetail.ISComm,"RequestPlayfieldList Answer received");
                Program.Ircbot.replyQueuePlayfieldList.Enqueue(requestPlayfieldList);
            }
        }

        /// <summary>
        /// The on client connected.
        /// </summary>
        /// <param name="client">
        /// </param>
        protected void ClientConnectedToChat(IClient client)
        {
            Client client1 = (Client)client;

            byte[] welcomePacket = new byte[]
                                   {
                                       0x00, 0x00, 0x00, 0x22, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                       // Server Salt (32 Bytes)
                                       0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                       0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                                   };

            // The seed is 32 lowercase hex characters, as the live chat server
            // sends it (every login seed in the captures, e.g. "c9ca2625f9b909884e
            // 79f85c854cfe2a"). It used to be 32 random bytes: the game client
            // coped, but chat bots read the seed as text, and Tyrbot failed on
            // bytes that are not valid UTF-8. ServerSalt stays the hex of the
            // bytes sent, which is what LoginEncryption rebuilds from the key.
            const string HexDigits = "0123456789abcdef";
            byte[] random = new byte[0x20];
            using (var generator = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                generator.GetBytes(random);
            }

            client1.ServerSalt = string.Empty;

            for (int i = 0; i < 32; i++)
            {
                byte seedCharacter = (byte)HexDigits[random[i] & 0x0F];

                welcomePacket[6 + i] = seedCharacter;

                client1.ServerSalt += string.Format("{0:x2}", seedCharacter);
            }

            client1.Send(welcomePacket);
        }

        /// <summary>
        /// The create client.
        /// </summary>
        /// <returns>
        /// </returns>
        protected override IClient CreateClient(IPAddress address)
        {
            return new Client(this);
        }

        /// <summary>
        /// The on receive udp.
        /// </summary>
        /// <param name="num_bytes">
        /// </param>
        /// <param name="buf">
        /// </param>
        /// <param name="ip">
        /// </param>
        protected override void OnReceiveUDP(int num_bytes, byte[] buf, IPEndPoint ip)
        {
        }

        /// <summary>
        /// The on send to.
        /// </summary>
        /// <param name="clientIP">
        /// </param>
        /// <param name="num_bytes">
        /// </param>
        protected override void OnSendTo(IPEndPoint clientIP, int num_bytes)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="vicinityChatMessage">
        /// </param>
        private void DistributeVicinityChat(VicinityChatMessage vicinityChatMessage)
        {
            byte[] packet = MsgVicinity.Create(
                (uint)vicinityChatMessage.SenderId,
                vicinityChatMessage.Text,
                (byte)vicinityChatMessage.MessageType);

            string lookup = CharacterDao.Instance.GetCharacterNameById(vicinityChatMessage.SenderId);
            byte[] nameLookup = NameLookupResult.Create((uint)vicinityChatMessage.SenderId, lookup);

            foreach (int charId in vicinityChatMessage.CharacterIds)
            {
                foreach (Client cli in this.ConnectedClients.Values)
                {
                    if (cli.Character.CharacterId == charId)
                    {
                        if (!cli.KnownClients.Contains((uint)vicinityChatMessage.SenderId))
                        {
                            // Name lookup
                            cli.Send(nameLookup);
                            cli.KnownClients.Add((uint)vicinityChatMessage.SenderId);
                        }

                        cli.Send(packet);
                    }
                }
            }
        }

        #endregion
    }
}
