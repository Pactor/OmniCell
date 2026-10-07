namespace ChatEngine.PacketHandlers
{
    #region Usings ...

    using System.Linq;

    using ChatEngine.CoreClient;
    using ChatEngine.Packets;

    #endregion

    /// <summary>
    /// Join private group
    /// </summary>
    public class PrivateGroupJoin
    {
        #region Fields

        /// <summary>
        /// The private group id (owner character id) being joined.
        /// </summary>
        private uint playerId;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Read private group join packet and, if this connection holds an invite
        /// to that group, add it to the group and tell every member.
        /// </summary>
        /// <param name="client">
        /// Client sending
        /// </param>
        /// <param name="packet">
        /// packet data
        /// </param>
        public void Read(Client client, byte[] packet)
        {
            PacketReader reader = new PacketReader(ref packet);

            reader.ReadUInt16(); // Packet ID
            reader.ReadUInt16(); // Data length
            this.playerId = reader.ReadUInt32();
            client.Server.Debug(
                client,
                "{0} >> PrivGrpJoin: PlayerID: {1}",
                client.Character.characterName,
                this.playerId);
            reader.Finish();

            uint joinerId = client.Character.CharacterId;

            lock (client.PendingPrivateGroupInvites)
            {
                if (!client.PendingPrivateGroupInvites.Remove(this.playerId))
                {
                    // No outstanding invite for this group; retail does not let
                    // a character join a private group it was not invited to.
                    return;
                }
            }

            if (!client.ChatServer().ConnectedClients.TryGetValue(this.playerId, out Client owner))
            {
                return; // owner logged off between the invite and the join
            }

            client.JoinedPrivateGroup = this.playerId;

            uint[] existing;
            lock (owner.PrivateGroupMembers)
            {
                existing = owner.PrivateGroupMembers.ToArray();
                owner.PrivateGroupMembers.Add(joinerId);
            }

            byte[] joinerJoined = PrivateGroupPlayerJoined.Create(this.playerId, joinerId);

            // Tell the owner and the existing members that the joiner is now in.
            PrivateGroupHelper.TellName(owner, client);
            owner.Send(joinerJoined);
            foreach (uint memberId in existing)
            {
                if (client.ChatServer().ConnectedClients.TryGetValue(memberId, out Client member))
                {
                    PrivateGroupHelper.TellName(member, client);
                    member.Send(joinerJoined);
                }
            }

            // Give the joiner the full roster: the owner, each existing member, and itself.
            PrivateGroupHelper.TellName(client, owner);
            client.Send(PrivateGroupPlayerJoined.Create(this.playerId, this.playerId));
            foreach (uint memberId in existing)
            {
                if (client.ChatServer().ConnectedClients.TryGetValue(memberId, out Client member))
                {
                    PrivateGroupHelper.TellName(client, member);
                }

                client.Send(PrivateGroupPlayerJoined.Create(this.playerId, memberId));
            }

            client.Send(joinerJoined);
        }

        #endregion
    }
}
