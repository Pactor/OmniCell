using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZoneEngine.Core.MessageHandlers
{
    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class ResearchUpdateMessageHandler : BaseMessageHandler<ResearchUpdateMessage,ResearchUpdateMessageHandler>
    {

        public void Send(ICharacter character, ResearchUpdateEntry[] entries)
        {
            this.Send(character,Filler(character.Identity,entries));
        }

        /// <summary>
        /// The research ids every captured entry lists, in the order it lists them.
        /// </summary>
        private static readonly int[] EntryResearchIds =
            {
                3000, 3010, 3011, 3012, 3020, 3021, 3022, 3030, 3040, 3041, 3042, 3050, 3051, 3052, 3060, 3070,
                3071, 3072, 3080, 3081, 3082, 3090, 3100, 3101, 3102, 3110, 3111, 3112, 3120, 3130, 3131, 3132,
                3140, 3141, 3142, 3200, 3201, 3202, 3203, 3204, 3205, 3206, 3207, 3208, 3209, 3210, 3211, 3212,
                3213, 3214, 3215, 3216, 3217, 3218
            };

        /// <summary>
        /// The ResearchUpdate a character is sent on entering the world, straight
        /// down its own client queue so it lands immediately before CharInPlay.
        /// </summary>
        /// <remarks>
        /// All 59 player entries in the retail captures carry one, and every one
        /// is the message directly before the player's CharInPlay. All 59 have the
        /// same body: format marker 1, Unknown 1, and these 54 research ids with
        /// all three remaining-experience values zero - for level 1 characters on
        /// new accounts and for a level 220 Keeper alike. The Identity is
        /// None:0 in every raw copy checked (20260911-171203_s12 seq 191,
        /// newchar_s9 seq 21, 475, 587, 141545 seq 96, perk_s6 seq 82), not the
        /// character.
        ///
        /// What those values mean for a character that has done research is not
        /// in any capture, so this sends the captured entry body and nothing
        /// derived from the character.
        /// </remarks>
        public void SendEntering(ICharacter character)
        {
            ResearchUpdateEntry[] entries =
                EntryResearchIds.Select(
                    id =>
                        new ResearchUpdateEntry
                        {
                            ResearchId = id,
                            NeutralResearchXpRemaining = 0,
                            ClanResearchXpRemaining = 0,
                            OmniResearchXpRemaining = 0
                        }).ToArray();
            this.Send(character, Filler(Identity.None, entries), false);
        }

        private MessageDataFiller Filler(Identity identity, ResearchUpdateEntry[] entries)
        {
            return x =>
            {
                x.Entries = entries;
                x.Identity = identity;
                x.FormatMarker = 1;
                x.Unknown = 1;
            };
        }
    }
}
