#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using System;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Database.Dao;
    using OmniCell.Enums;
    using OmniCell.Stats;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.PacketHandlers;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class CharacterInfoPacketMessageHandler :
        BaseMessageHandler<InfoPacketMessage, CharacterInfoPacketMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="infoTarget">
        /// </param>
        public void Send(ICharacter character, ICharacter infoTarget)
        {
            this.Send(character, CharacterInfoPacket(character, infoTarget), false);
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="tPlayer">
        /// </param>
        /// <returns>
        /// </returns>
        private static MessageDataFiller CharacterInfoPacket(ICharacter character, ICharacter tPlayer)
        {
            return x =>
            {
                uint LegacyScore = tPlayer.Stats[StatIds.pvp_rating].BaseValue;
                string LegacyTitle = null;
                if (LegacyScore < 1400)
                {
                    LegacyTitle = string.Empty;
                }
                else if (LegacyScore < 1500)
                {
                    LegacyTitle = "Freshman";
                }
                else if (LegacyScore < 1600)
                {
                    LegacyTitle = "Rookie";
                }
                else if (LegacyScore < 1700)
                {
                    LegacyTitle = "Apprentice";
                }
                else if (LegacyScore < 1800)
                {
                    LegacyTitle = "Novice";
                }
                else if (LegacyScore < 1900)
                {
                    LegacyTitle = "Neophyte";
                }
                else if (LegacyScore < 2000)
                {
                    LegacyTitle = "Experienced";
                }
                else if (LegacyScore < 2100)
                {
                    LegacyTitle = "Expert";
                }
                else if (LegacyScore < 2300)
                {
                    LegacyTitle = "Master";
                }
                else if (LegacyScore < 2500)
                {
                    LegacyTitle = "Champion";
                }
                else
                {
                    LegacyTitle = "Grand Master";
                }

                int orgGoverningForm = 0;
                try
                {
                    orgGoverningForm = OrganizationDao.Instance.GetGovernmentForm(character.Stats[StatIds.clan].Value);
                }
                catch (Exception)
                {
                }

                // Uses methods in ZoneEngine\PacketHandlers\OrgClient.cs
                /* Known packetFlags--
                    * 0x40 - No org | 0x41 - Org | 0x43 - Org and towers | 0x47 - Org, towers, player has personal towers | 0x50 - No pvp data shown
                    * Bitflags--
                    * Bit0 = hasOrg, Bit1 = orgTowers, Bit2 = personalTowers, Bit3 = (Int32) time until supression changes (Byte) type of supression level?, Bit4 = noPvpDataShown, Bit5 = hasFaction, Bit6 = ?, Bit 7 = null.
                */

                int orgId;
                string orgRank;
                InfoPacketFlags flags = InfoPacketFlags.Versioned;
                if (tPlayer.Stats[StatIds.clan].BaseValue == 0)
                {
                    // The organization id is not optional. It is the fourth of
                    // four unconditional int32s and a character with no
                    // organization sends a zero there; only the rank string and
                    // the city playfield hang off the 0x01 bit.
                    orgId = 0;
                    orgRank = null;
                }
                else
                {
                    flags |= InfoPacketFlags.Organization;
                    orgId = (int)tPlayer.Stats[StatIds.clan].BaseValue;
                    if (character.Stats[StatIds.clan].BaseValue == tPlayer.Stats[StatIds.clan].BaseValue)
                    {
                        orgRank = OrgClient.GetRank(orgGoverningForm, tPlayer.Stats[StatIds.clanlevel].BaseValue);
                    }
                    else
                    {
                        orgRank = string.Empty;
                    }
                }

                if (tPlayer.Stats[StatIds.npcfamily].Value != 0)
                {
                    flags |= InfoPacketFlags.NotAPlayer;
                    x.Unknown = 1;
                    // A monster has no profession, and saying it has one the
                    // client has never heard of ends the session. Nothing sets
                    // the stat on a spawn, so it held the stat list's unset
                    // marker, and a byte of 1234567890 is 210 - which is what
                    // went out for an Anger Manifestation and took the client
                    // down the moment anyone clicked it. The captures have 0
                    // here for every monster, and 9 for a few named ones.
                    //
                    // The three trailing markers are not a mistake: the live
                    // server sends 1234567890 in all three, every time.
                    x.Info = new InfoPacket
                             {
                                 Version = 1,
                                 SideXp = 0,
                                 FirstName = string.Empty,
                                 LastName = string.Empty,
                                 AuxiliaryName = string.Empty,
                                 DisplayText = string.Empty,
                                 Health = StatValue.OrZero(tPlayer.Stats[StatIds.health].Value),
                                 Level = (byte)Math.Max(1, StatValue.OrZero(tPlayer.Stats[StatIds.level].Value)),
                                 MaxHealth = StatValue.OrZero(tPlayer.Stats[StatIds.life].Value),
                                 OrganizationId = 0,
                                 Profession = KnownProfession(tPlayer.Stats[StatIds.profession].Value),
                                 TitleLevel =
                                     (byte)Math.Max(1, StatValue.OrZero(tPlayer.Stats[StatIds.titlelevel].Value)),
                                 VisualProfession =
                                     KnownProfession(tPlayer.Stats[StatIds.visualprofession].Value),
                                 InvadersKilled = 1234567890,
                                 KilledByInvaders = 1234567890,
                                 AiLevel = 1234567890,
                             };
                }
                else
                {
                    x.Unknown = 0;
                    x.Info = new InfoPacket
                             {
                                 Version = 0x01,
                                 // Guarded the same way as the monster branch
                                 // above. A player always has these set, but a
                                 // level of 210 is not the kind of thing to find
                                 // out about from a client that has gone.
                                 Profession = KnownProfession(tPlayer.Stats[StatIds.profession].Value),
                                 Level = (byte)Math.Max(1, StatValue.OrZero(tPlayer.Stats[StatIds.level].Value)),
                                 TitleLevel =
                                     (byte)Math.Max(1, StatValue.OrZero(tPlayer.Stats[StatIds.titlelevel].Value)),
                                 VisualProfession =
                                     KnownProfession(tPlayer.Stats[StatIds.visualprofession].Value),
                                 SideXp = 0,
                                 Health = StatValue.OrZero(tPlayer.Stats[StatIds.health].Value),
                                 MaxHealth = StatValue.OrZero(tPlayer.Stats[StatIds.life].Value),
                                 BreedHostility = 0x00000000,
                                 OrganizationId = orgId,
                                 FirstName = tPlayer.FirstName,
                                 LastName = tPlayer.LastName,
                                 AuxiliaryName = LegacyTitle,
                                 DisplayText = string.Empty,
                                 OrganizationRank = orgRank,
                                 GridDestinations = null,
                                 CityPlayfieldId = orgRank == null ? (int?)null : 0x00000000,
                                 AcgItems = null,
                                 InvadersKilled = StatValue.OrZero(tPlayer.Stats[StatIds.invaderskilled].Value),
                                 KilledByInvaders = StatValue.OrZero(tPlayer.Stats[StatIds.killedbyinvaders].Value),
                                 AiLevel = StatValue.OrZero(tPlayer.Stats[StatIds.alienlevel].Value),
                                 PvpDuelKills = StatValue.OrZero(tPlayer.Stats[StatIds.pvpduelkills].Value),
                                 PvpDuelDeaths = StatValue.OrZero(tPlayer.Stats[StatIds.pvpdueldeaths].Value),
                                 PvpProfessionDuelKills =
                                     StatValue.OrZero(tPlayer.Stats[StatIds.pvpprofessiondueldeaths].Value),
                                 PvpRankedSoloKills = StatValue.OrZero(tPlayer.Stats[StatIds.pvprankedsolokills].Value),
                                 PvpRankedTeamKills = StatValue.OrZero(tPlayer.Stats[StatIds.pvprankedteamkills].Value),
                                 PvpSoloScore = StatValue.OrZero(tPlayer.Stats[StatIds.pvpsoloscore].Value),
                                 PvpTeamScore = StatValue.OrZero(tPlayer.Stats[StatIds.pvpteamscore].Value),
                                 PvpDuelScore = StatValue.OrZero(tPlayer.Stats[StatIds.pvpduelscore].Value)
                             };
                }

                x.Flags = flags;
                
                x.Identity = tPlayer.Identity;
            };
        }

        /// <summary>
        /// A profession the client will recognise, or none.
        /// </summary>
        /// <remarks>
        /// There are fifteen of them. Anything else - an unset stat, a spawn
        /// that was never given one - is nobody's profession, and the captures
        /// send zero for exactly that case.
        /// </remarks>
        private static byte KnownProfession(int profession)
        {
            int known = StatValue.OrZero(profession);
            return known < 1 || known > 15 ? (byte)0 : (byte)known;
        }

        #endregion

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="identity">
        /// </param>
        internal void Send(ICharacter character, Identity identity)
        {
            // Only for Characters now
            // Need more info whether to send for non Characters too
            var obj = Pool.Instance.GetObject(character.Playfield.Identity, identity) as ICharacter;

            if (obj != null)
            {
                if (obj.Stats[StatIds.npcfamily].Value == 0)
                {
                    // !=0 -> Monster
                    this.Send(character, obj);
                }
            }
        }
    }
}