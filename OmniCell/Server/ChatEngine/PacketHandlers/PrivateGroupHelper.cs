namespace ChatEngine.PacketHandlers
{
    #region Usings ...

    using ChatEngine.CoreClient;
    using ChatEngine.Packets;

    #endregion

    /// <summary>
    /// Shared helpers for the private group (private channel) handlers.
    /// </summary>
    internal static class PrivateGroupHelper
    {
        /// <summary>
        /// Makes sure <paramref name="recipient"/> can resolve the name of
        /// <paramref name="who"/>, sending the name packet once (the same guard
        /// Tell uses). A client that cannot resolve a character id shows it as a
        /// raw number instead of a name.
        /// </summary>
        public static void TellName(Client recipient, Client who)
        {
            uint id = who.Character.CharacterId;
            if (!recipient.KnownClients.Contains(id))
            {
                recipient.Send(PlayerName.Create(who, id));
                recipient.KnownClients.Add(id);
            }
        }
    }
}
