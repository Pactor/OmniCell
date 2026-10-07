namespace ChatEngine.PacketHandlers
{
    #region Usings ...

    using ChatEngine.CoreClient;
    using ChatEngine.Packets;

    #endregion

    /// <summary>
    /// The private group invite player.
    /// </summary>
    public class PrivateGroupInvitePlayer
    {
        #region Fields

        /// <summary>
        /// The playerId being invited.
        /// </summary>
        private uint playerId;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Read private group invite player packet and invite the target into the
        /// private group this connection owns (group id == this character's id).
        /// </summary>
        /// <param name="client">
        /// Client sending
        /// </param>
        /// <param name="packet">
        /// Packet data
        /// </param>
        public void Read(Client client, byte[] packet)
        {
            PacketReader reader = new PacketReader(ref packet);

            reader.ReadUInt16(); // Packet ID
            reader.ReadUInt16(); // Data length
            this.playerId = reader.ReadUInt32();
            client.Server.Debug(
                client,
                "{0} >> PrivGrpInvitePlayer: PlayerID: {1}",
                client.Character.characterName,
                this.playerId);
            reader.Finish();

            uint groupId = client.Character.CharacterId;
            if (this.playerId == groupId)
            {
                return;
            }

            if (!client.ChatServer().ConnectedClients.TryGetValue(this.playerId, out Client target))
            {
                client.Send(MsgSystem.Create("Player not online."));
                return;
            }

            // So each side can resolve the other's name, the same courtesy Tell pays.
            PrivateGroupHelper.TellName(target, client);

            lock (target.PendingPrivateGroupInvites)
            {
                target.PendingPrivateGroupInvites.Add(groupId);
            }

            // Packet 50 outbound: the target is invited to groupId's private group.
            target.Send(PrivateGroupInvitation.Create(groupId));
        }

        #endregion
    }
}
