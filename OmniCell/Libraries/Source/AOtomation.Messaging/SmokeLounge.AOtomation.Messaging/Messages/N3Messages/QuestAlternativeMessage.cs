using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System.Dynamic;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The missions a terminal is offering.
    /// </summary>
    /// <remarks>
    /// Almost every field here is named by one error message. When the client
    /// refuses one of these it prints what it could not accept, and the format
    /// string at Gamecode 0x1016BB20 names the fields as it goes:
    ///
    ///   ERROR: invalid data for ACGQuestIIR_t %u:%u in stream:
    ///          Difficulty       = %u (valid is [%d, %d])
    ///          Dimension values = %s
    ///          Seed             = %u
    ///          Originator       = %u:%u %u (valid is [%d, %d])
    ///
    /// The arguments it pushes at 0x100CA062 line those up with the fields the
    /// reader filled: the difficulty is the byte before the sliders, the
    /// dimension values are the six sliders, the seed is the int32, and the
    /// originator is the identity and the byte after them.
    ///
    /// The two ranges are [1, 11] and [1, 8], not [0, 11] and [0, 8] as this
    /// remark said until 2026-09-12. The pushes are the bounds themselves -
    /// `push 0xb; push edi` and `push 8; push edi` with edi set to one at
    /// 0x100CA066 - and the reader agrees, taking the value minus one and
    /// refusing anything above ten. Zero is not a difficulty and not an
    /// originator; a server that sends one gets the packet dropped.
    /// </remarks>
    [AoContract((int)N3MessageType.QuestAlternative)]
    public class QuestAlternativeMessage:N3Message
    {
        public QuestAlternativeMessage()
        {
            this.N3MessageType = N3MessageType.QuestAlternative;
        }

        /// <summary>
        /// The record's version. 4 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Read at 0x100C9F88 and checked against the class's own version
        /// through its vtable; a mismatch is printed and the message dropped.
        /// </remarks>
        [AoMember(0)]
        public byte VersionId { get; set; } 

        /// <summary>
        /// How hard the missions on offer are, from 1 to 11.
        /// </summary>
        /// <remarks>
        /// The error message calls it the difficulty and gives its range, and
        /// the captured terminals offer 6, 11 and 9 - inside it. Read before
        /// the sliders, at 0x100C9FCC, which is what makes it not one of them.
        /// </remarks>
        [AoMember(1)]
        public byte Difficulty { get; set; }
        /// <summary>
        /// "good vs bad (GB)", from -100 to 100.
        /// </summary>
        /// <remarks>
        /// This and the five below are one GameData::DimensionValues_t, read at
        /// 0x100C9FD3 through GameData's own stream operator. That operator
        /// takes six bytes and refuses any of them outside -100 to 100: it adds
        /// 100 to each and rejects anything above 200.
        ///
        /// The names are GameData's as well. GetDimensionName at GameData.dll
        /// 0x1000219B is a table of six strings and these are them, in order.
        ///
        /// They are signed on the wire, which is why the captured 229 and 250
        /// are not out of range - they are -27 and -6. They are bytes here
        /// because the serializer has no sbyte, so the sign has to be applied
        /// by whoever reads them.
        /// </remarks>
        [AoMember(2)]
        public byte GoodBad { get; set; }
        /// <summary>
        /// "controlled vs lacking control (CL)", from -100 to 100.
        /// </summary>
        /// <remarks>
        /// Called OrderChaos here before the client was asked. See
        /// <see cref="GoodBad"/>.
        /// </remarks>
        [AoMember(3)]
        public byte ControlledLackingControl { get; set; }
        /// <summary>
        /// "open vs hidden (OH)", from -100 to 100. See
        /// <see cref="GoodBad"/>.
        /// </summary>
        [AoMember(4)]
        public byte OpenHidden { get; set; }
        /// <summary>
        /// "physical vs mystical (PM)", from -100 to 100. See
        /// <see cref="GoodBad"/>.
        /// </summary>
        [AoMember(5)]
        public byte PhysicalMystical { get; set; }
        /// <summary>
        /// "explosive vs patient (EP)", from -100 to 100.
        /// </summary>
        /// <remarks>
        /// Called HeadOnStealth here before the client was asked. See
        /// <see cref="GoodBad"/>.
        /// </remarks>
        [AoMember(6)]
        public byte ExplosivePatient { get; set; }
        /// <summary>
        /// "money reward vs experience reward (ME)", from -100 to 100. See
        /// <see cref="GoodBad"/>.
        /// </summary>
        [AoMember(7)]
        public byte MoneyExperience { get; set; }
        
        // Maybe this is the last Random which generated this mission packet
        /// <summary>
        /// What the missions on offer were generated from.
        /// </summary>
        /// <remarks>
        /// The error message calls it the seed. A different large value in each
        /// of the four captured terminals, which is what a seed looks like.
        /// </remarks>
        [AoMember(8)]
        public int Seed { get; set; }

        /// <summary>
        /// What kind of place is offering them.
        /// </summary>
        /// <remarks>
        /// The error message gives its range as [0, 8], which is the span of
        /// GameData's QuestOriginator_e exactly, and every captured copy
        /// carries 1 - a quest from a neutral booth - beside a mission terminal
        /// identity.
        /// </remarks>
        [AoMember(9)]
        public QuestOriginator Originator { get; set; }
        [AoMember(10)]
        public Identity MissionTerminalIdentity { get; set; }

        /// <summary>
        /// The quests on offer, each followed by a byte.
        /// </summary>
        /// <remarks>
        /// A byte count, then that many quests - and the client takes a byte
        /// after every one of them, inside the loop at 0x100CB2EE, rather than
        /// once after the list the way QuestFullUpdate does. That is what
        /// <see cref="QuestAlternativeEntry"/> exists to say.
        /// </remarks>
        [AoMember(11, SerializeSize = ArraySizeType.Byte)]
        public QuestAlternativeEntry[] QuestInfos { get; set; }




    }
}
