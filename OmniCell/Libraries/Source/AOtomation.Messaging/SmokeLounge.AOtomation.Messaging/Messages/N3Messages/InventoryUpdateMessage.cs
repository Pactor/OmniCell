namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// A container and everything in it.
    /// </summary>
    /// <remarks>
    /// The client calls the container NewInventory_t and builds it in the
    /// reader at 0x100A0859 from the first two fields: the capacity and what
    /// may be done with it. The dispatcher at 0x100A0957 resolves the bag
    /// identity, and when it turns out to be a Chest_t it copies the access
    /// bits onto the chest's own inventory, puts the chest in the owner's
    /// inventory at the slot this message names, and - if the last field is set
    /// - opens the window.
    /// </remarks>
    [AoContract((int)N3MessageType.InventoryUpdate)]
    public class InventoryUpdateMessage : N3Message
    {
        public InventoryUpdateMessage()
        {
            this.N3MessageType = N3MessageType.InventoryUpdate;
        }

        /// <summary>
        /// How many slots the container has.
        /// </summary>
        /// <remarks>
        /// Kept at NewInventory_t's +0x14 and used as the bound on every slot
        /// lookup, so a slot number at or above this one reads as empty.
        /// </remarks>
        [AoMember(0)]
        public int NumberOfSlots { get; set; }

        /// <summary>
        /// Whether things may be put into this container and taken out of it.
        /// </summary>
        /// <remarks>
        /// Every captured copy is a corpse and carries 2 - take only. What the
        /// client builds for itself is 3, except the overflow window, which is
        /// also 2.
        /// </remarks>
        [AoMember(1)]
        public InventoryAccess Access { get; set; }

        /// <summary>
        /// </summary>
        [AoMember(2, SerializeSize = ArraySizeType.X3F1)]
        public InventoryEntry[] Entries { get; set; }

        /// <summary>
        /// </summary>
        [AoMember(3)]
        public Identity BagIdentity { get; set; }

        /// <summary>
        /// Which slot of the owner's inventory the bag sits in.
        /// </summary>
        /// <remarks>
        /// The dispatcher stores it on the chest at +0x1DC and hands it to
        /// 0x1004B4B5, which is what puts the chest in the character's
        /// inventory. 112 to 131 across the captures.
        /// </remarks>
        [AoMember(4)]
        public int SlotnumberInMainInventory { get; set; }

        /// <summary>
        /// Anything but zero opens the container's window.
        /// </summary>
        /// <remarks>
        /// Four bytes on the wire and a bool to the client, which reads it with
        /// a setne. When it is set the dispatcher clears bit 0x40 of the
        /// chest's +0x4C and calls the chest's vtable +0xAC, which sets two
        /// flags and reads stat 435, ReadOnly - that is the window opening. 1
        /// in all 59 captured copies, which is what a corpse you just clicked
        /// looks like.
        /// </remarks>
        [AoMember(5)]
        public int Open { get; set; }
    }
}