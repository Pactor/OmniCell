// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientContainerAddItemMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ClientContainerAddItemMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Put an item into a container.
    /// </summary>
    /// <remarks>
    /// Two identities. Reader 0x1001518B, writer 0x100151BA, constructor
    /// 0x100151E7, dispatcher 0x100151E4 - a bare return, this being a message
    /// the client only sends; vtable 0x101582DC. Class name
    /// ClientContainerAddItemIIR_t.
    ///
    /// Which identity is which is settled by the one function that builds the
    /// message. N3Msg_ContainerAddItem(Identity const&amp;, Identity const&amp;),
    /// exported at 0x10028433, hands its first argument to
    /// N3Msg_GetContainerInventoryList at 0x100175A6 and its second to
    /// N3Msg_IsItemPossibleToUnWear at 0x100268DF - so the first is the
    /// container and the second the item - and the constructor puts them on the
    /// wire in that order.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.ClientContainerAddItem)]
    public class ClientContainerAddItemMessage : N3Message
    {
        #region Constructors and Destructors

        public ClientContainerAddItemMessage()
        {
            this.N3MessageType = N3MessageType.ClientContainerAddItem;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The container to put it in.
        /// </summary>
        [AoMember(0)]
        public Identity Container { get; set; }

        /// <summary>
        /// The item going in.
        /// </summary>
        [AoMember(1)]
        public Identity Item { get; set; }

        #endregion
    }
}
