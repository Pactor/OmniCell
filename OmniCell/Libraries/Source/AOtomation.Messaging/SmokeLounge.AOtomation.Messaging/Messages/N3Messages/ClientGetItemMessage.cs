// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientGetItemMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ClientGetItemMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Pick an item up off the ground.
    /// </summary>
    /// <remarks>
    /// One Identity and nothing else. Reader 0x10015285, writer 0x100152A5,
    /// constructor 0x100152C1, dispatcher 0x100152BE - which does nothing, this
    /// being a message the client only sends; vtable 0x10158320. The
    /// constructor registers the class name ClientGetItemIIR_t, which is what
    /// the id hashes from.
    ///
    /// What the Identity is comes from the only place the message is built.
    /// N3Msg_GetItem(Identity const&amp;), exported at 0x10027D76, looks for room
    /// in inventory page 0x40 first and says so if there is none, resolves its
    /// argument to an item at 0x10087758, and then hands that same argument to
    /// the constructor. So it is the item being picked up.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.ClientGetItem)]
    public class ClientGetItemMessage : N3Message
    {
        #region Constructors and Destructors

        public ClientGetItemMessage()
        {
            this.N3MessageType = N3MessageType.ClientGetItem;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The item to pick up.
        /// </summary>
        [AoMember(0)]
        public Identity Item { get; set; }

        #endregion
    }
}
