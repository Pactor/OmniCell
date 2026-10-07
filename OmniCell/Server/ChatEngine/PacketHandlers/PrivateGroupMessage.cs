namespace ChatEngine.PacketHandlers
{
    #region Usings ...

    using System.Linq;

    using ChatEngine.CoreClient;

    #endregion

    /// <summary>
    /// Private Group Message
    /// </summary>
    public class PrivateGroupMessage
    {
        #region Fields

        /// <summary>
        /// The message body.
        /// </summary>
        private string message = string.Empty;

        /// <summary>
        /// The extended message blob.
        /// </summary>
        private string blob = string.Empty;

        /// <summary>
        /// The private group id (owner character id) the message is sent to.
        /// </summary>
        private uint groupId;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Read a private group message and fan it out to every member of the
        /// group (the owner plus everyone joined), the sender included, which is
        /// how the retail chat server echoes it back.
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
            this.groupId = reader.ReadUInt32();
            this.message = reader.ReadString();
            this.blob = reader.ReadString();
            client.Server.Debug(
                client,
                "{0} >> PrivGrpMessage: Group: {1} Message: {2}",
                client.Character.characterName,
                this.groupId,
                this.message);
            reader.Finish();

            uint senderId = client.Character.CharacterId;

            if (!client.ChatServer().ConnectedClients.TryGetValue(this.groupId, out Client owner))
            {
                return; // the group owner is not online
            }

            uint[] members;
            bool isMember;
            lock (owner.PrivateGroupMembers)
            {
                isMember = senderId == this.groupId || owner.PrivateGroupMembers.Contains(senderId);
                members = owner.PrivateGroupMembers.ToArray();
            }

            if (!isMember)
            {
                return; // only the owner or a joined member may talk in the group
            }

            byte[] outPacket = Packets.PrivateGroupMessage.Create(this.groupId, senderId, this.message, this.blob);

            // Owner first, then each joined member. Everyone, sender included.
            PrivateGroupHelper.TellName(owner, client);
            owner.Send(outPacket);
            foreach (uint memberId in members)
            {
                if (client.ChatServer().ConnectedClients.TryGetValue(memberId, out Client member))
                {
                    PrivateGroupHelper.TellName(member, client);
                    member.Send(outPacket);
                }
            }
        }

        #endregion
    }
}
