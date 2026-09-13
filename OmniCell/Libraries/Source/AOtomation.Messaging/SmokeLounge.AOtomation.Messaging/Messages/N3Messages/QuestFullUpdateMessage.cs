using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using SmokeLounge.AOtomation.Messaging.Serialization.Serializers;

    [AoContract((int)N3MessageType.QuestFullUpdate)]
    public class QuestFullUpdateMessage : N3Message
    {
        public QuestFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.QuestFullUpdate;
        }


        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public QuestInfo[] QuestInfos { get; set; }

        /// <summary>
        /// Whether to announce each of these to the player as a new mission.
        /// </summary>
        /// <remarks>
        /// The reader at 0x100ACF68 reads the list and then takes a single byte
        /// at 0x100ACF7E, compares it against zero and keeps the answer as a
        /// bool on the message. This byte used to sit on QuestInfo, which is
        /// the same bytes on the wire for a message carrying one quest and
        /// wrong for any other - and every captured copy carries one except
        /// one, which carries three. In that copy the second and third quests
        /// came out with a version of 3840, which is 15 read one byte late.
        ///
        /// The packet still round tripped either way, because a model that
        /// reads its own mistake back writes it out again unchanged. What
        /// caught it was the second quest in a list having a version no quest
        /// has.
        ///
        /// What it is for comes from the dispatcher at 0x100ACFDE, which walks
        /// the list and calls 0x10056CDD once per quest with this byte as its
        /// second argument. That function appends the quest to the character's
        /// list and then, only when the byte is non-zero and the character is
        /// the one the player controls, raises the feedback string
        /// Feedback_YouGotANewMission and emits GlobalSignals_c + 0x154 with
        /// the quest's identity. So it is a per-send announcement flag, and
        /// setting it on a window refresh tells the player they have just been
        /// given every quest they were already on.
        ///
        /// No copy of this message appears in the capture set, so which way
        /// retail sets it cannot be checked here; the name and the effect are
        /// the client's.
        /// </remarks>
        [AoMember(2)]
        public byte AnnounceAsNew { get; set; }

    }
}
