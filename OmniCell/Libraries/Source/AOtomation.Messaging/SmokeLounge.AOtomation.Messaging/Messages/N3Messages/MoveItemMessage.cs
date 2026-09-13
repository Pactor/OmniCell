// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoveItemMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MoveItemMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Sent by the client to move an item from one place to another.
    /// </summary>
    /// <remarks>
    /// Not part of the original SmokeLounge work. This message id appears in
    /// every capture taken from a live 18.8.x client and was reconstructed from
    /// those, by marking actions as they were performed and reading back what
    /// went out.
    ///
    /// It covers equipping, unequipping and looting alike - anything that moves
    /// an item between slots. Observed cases, source on the left:
    ///
    ///   Identity(Inventory, 0x46) -> 0x06   equipping a weapon
    ///   Identity(Inventory, 0x43) -> 0x12   equipping glasses
    ///   Identity(Inventory, 0x4b) -> 0x13   equipping a shirt
    ///   Identity(Inventory, 0x4f) -> 0x2b   placing an implant
    ///   Identity(Backpack,  ...)  -> 0x6f   taking loot from a corpse
    ///
    /// The first two of those were byte identical across two sessions on
    /// different characters, which is what establishes the operands as fixed
    /// slots rather than world instances.
    ///
    /// The client sends this only once it is satisfied the move is allowed. A
    /// player who lacks the skill to equip something gets no message at all -
    /// the client checks requirements against its own item data and stays
    /// silent. Confirmed for weapons, nano programs and skill raises.
    /// </remarks>
    [AoContract((int)N3MessageType.MoveItem)]
    public class MoveItemMessage : N3Message
    {
        #region Constructors and Destructors

        public MoveItemMessage()
        {
            this.N3MessageType = N3MessageType.MoveItem;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Where the item is now. For Inventory the instance is the slot; for a
        /// container such as a Backpack it identifies the container itself.
        /// </summary>
        [AoMember(0)]
        public Identity Source { get; set; }

        /// <summary>
        /// The slot the item is being moved into.
        /// </summary>
        [AoMember(1)]
        public int Destination { get; set; }

        #endregion
    }
}
