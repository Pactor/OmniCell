namespace ChatEngine.PacketHandlers
{
    #region Usings ...

    using System.Linq;

    using ChatEngine.CoreClient;
    using ChatEngine.Packets;

    #endregion

    /// <summary>
    /// The private group leave.
    /// </summary>
    public class PrivateGroupLeave
    {
        #region Fields

        /// <summary>
        /// The private group id (owner character id) being left.
        /// </summary>
        private uint playerId;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Read private group leave packet and remove this connection from the
        /// group, telling the owner and the remaining members.
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
                "{0} >> PrivGrpLeave: PlayerId: {1}",
                client.Character.characterName,
                this.playerId);
            reader.Finish();

            uint leaverId = client.Character.CharacterId;
            uint groupId = this.playerId != 0 ? this.playerId : client.JoinedPrivateGroup;
            if (groupId == 0)
            {
                return;
            }

            client.JoinedPrivateGroup = 0;

            if (!client.ChatServer().ConnectedClients.TryGetValue(groupId, out Client owner))
            {
                return;
            }

            uint[] remaining;
            lock (owner.PrivateGroupMembers)
            {
                if (!owner.PrivateGroupMembers.Remove(leaverId))
                {
                    return;
                }

                remaining = owner.PrivateGroupMembers.ToArray();
            }

            byte[] left = PrivateGroupPlayerLeft.Create(groupId, leaverId);
            client.Send(left);
            owner.Send(left);
            foreach (uint memberId in remaining)
            {
                if (client.ChatServer().ConnectedClients.TryGetValue(memberId, out Client member))
                {
                    member.Send(left);
                }
            }
        }

        #endregion
    }
}
