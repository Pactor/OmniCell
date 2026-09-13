// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InspectMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InspectMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// What another character is wearing.
    /// </summary>
    /// <remarks>
    /// The answer to a look at somebody. The client asks with a
    /// CharacterAction: N3Msg_Inspect(Identity const&amp;) at Gamecode
    /// 0x1001DDD4 does nothing but send action 0x105 with the target, and this
    /// comes back.
    ///
    /// Reader 0x100750C4, writer 0x10075125, dispatcher 0x1007514A, vtable
    /// 0x10162070. The reader takes an Identity, then makes a container of its
    /// own - the constructor at 0x1002B03C, with page 0x40 and that same
    /// Identity, neither of which is on the wire - and fills it with the shared
    /// container reader at 0x1002A5DB. The writer emits the Identity and the
    /// container and nothing else, so the two agree.
    ///
    /// The dispatcher says what the Identity is for. It resolves it to a
    /// character, stops if that character is on its way out, takes that
    /// character&#39;s own inventory at +0x1B8, empties it at 0x1002AB3D and
    /// then walks the entries this message carried into it one at a time at
    /// 0x1002AC62. So the Identity is whoever was inspected, and the container
    /// is what they are wearing - which is why the client throws away what it
    /// had for them first.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.Inspect)]
    public class InspectMessage : N3Message
    {
        #region Constructors and Destructors

        public InspectMessage()
        {
            this.N3MessageType = N3MessageType.Inspect;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The character being inspected.
        /// </summary>
        /// <remarks>
        /// Read at 0x100750DA, handed to the container the reader builds, and
        /// resolved by the dispatcher into the character whose inventory is
        /// replaced.
        /// </remarks>
        [AoMember(0)]
        public Identity Target { get; set; }

        /// <summary>
        /// What they are wearing.
        /// </summary>
        /// <remarks>
        /// Read at 0x1007510C by the shared container reader. Each entry is a
        /// placement, two int16s, an Identity and a GameData::ACGItem_t - the
        /// same record every other container carries.
        /// </remarks>
        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Contents { get; set; }

        #endregion
    }
}
