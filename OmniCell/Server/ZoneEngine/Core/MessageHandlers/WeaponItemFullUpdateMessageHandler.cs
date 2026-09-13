#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using System.Collections.Generic;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Items;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;
    using OmniCell.Stats;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Playfields;

    #endregion

    /// <summary>
    /// Puts a weapon in a character's hands, as far as the client is concerned.
    /// </summary>
    /// <remarks>
    /// The live server sends one of these immediately after the
    /// SimpleCharFullUpdate of any armed character - 2454 of them across the
    /// captured sessions, more than any message except the movement and stat
    /// traffic. OmniCell never sent one, so every character in the world stood
    /// there empty handed.
    ///
    /// Several fields are the same in every capture: MsgVersion 11, state
    /// machine type 1000015, and the run 23, 701, 702, 703, 412, 26 threaded
    /// between the item values. Inventory id and body location are separate
    /// captured bytes and are not fixed to one hand. That run is stat ids - StaticInstance,
    /// ACGItemLevel, ACGItemTemplateID, ACGItemTemplateID2, MultipleCount - so
    /// the message is a stat list, and the "AcgItemLevel" style
    /// names on the message class are the ids rather than the values. They are
    /// written as observed rather than renamed, because renaming a field of a
    /// message that is understood only by its byte layout is how a working
    /// packet becomes a broken one.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class WeaponItemFullUpdateMessageHandler :
        BaseMessageHandler<WeaponItemFullUpdateMessage, WeaponItemFullUpdateMessageHandler>
    {
        #region Constants

        /// <summary>
        /// Constant in every captured WeaponItemFullUpdate.
        /// </summary>
        private const int Version = 11;

        /// <summary>
        /// The identity type of a state machine. A weapon has none, so the
        /// instance beside it is zero.
        /// </summary>
        private const int StateMachineType = 1000015;

        private const int StatStaticInstance = 23;

        private const int StatAcgItemLevel = 701;

        private const int StatAcgItemTemplateId = 702;

        private const int StatAcgItemTemplateId2 = 703;

        private const int StatMultipleCount = 412;

        private const int StatEnergy = 26;

        /// <summary>
        /// Ammo, minus one in every capture of a melee weapon.
        /// </summary>
        private const int NoAmmo = -1;

        #endregion

        #region Outbound

        /// <summary>
        /// Sends one character's weapon to one client.
        /// </summary>
        public void Send(ICharacter to, ICharacter wielder, DBMobSpawnWeapon weapon)
        {
            this.Send(to, Filler(wielder, weapon), false);
        }

        /// <summary>
        /// Announces one character's weapon to the whole playfield.
        /// </summary>
        public void Announce(ICharacter wielder, DBMobSpawnWeapon weapon)
        {
            this.Send(wielder, Filler(wielder, weapon), true);
        }

        /// <summary>
        /// Gives a stable session-independent identity to a player weapon whose
        /// old database row has no instance id.  The exact live allocator is
        /// unknown; only the required type, uniqueness and cross-packet match
        /// are inferred here.
        /// </summary>
        public static Identity PlayerWeaponIdentity(ICharacter character, int placement)
        {
            uint mixed = unchecked((uint)character.Identity.Instance * 16777619u) ^ unchecked((uint)placement);
            return new Identity
                       {
                           Type = IdentityType.WeaponInstance,
                           Instance = unchecked((int)(0x20000000u | (mixed & 0x1fffffffu)))
                       };
        }

        /// <summary>
        /// Sends the definition of a player weapon referenced by FullCharacter.
        /// Placement is its flattened FullCharacter slot number.
        /// </summary>
        public void Send(ICharacter character, IItem weapon, int placement)
        {
            this.SendPlayerWeapon(character, weapon, placement, 0x100 + placement);
        }

        /// <summary>
        /// Defines a player weapon while it is being equipped, unless the client
        /// already has that weapon defined this session.  Live uses the bare
        /// destination slot here; the later login snapshot uses 0x100 plus the
        /// slot instead.
        /// </summary>
        /// <remarks>
        /// Live does not repeat a definition the client already holds. Taking a
        /// weapon off and putting it back on is CharacterAction Unknown3,
        /// ContainerAddItem, stance, Equip, with no WeaponItemFullUpdate between
        /// - twice in perk_s6 (seq 17672-17675) and once in 20260913-070754 (seq
        /// 642-644) - while a weapon new to the session gets one (perk_s6 seq
        /// 2733, newchar_s9 seq 115). The identity here is per slot, so "already
        /// defined" means this identity was last sent with this same item.
        /// </remarks>
        public void SendForEquip(ICharacter character, IItem weapon, int placement)
        {
            Identity identity = PlayerWeaponIdentity(character, placement);
            string item = weapon.LowID + "/" + weapon.HighID + "/" + weapon.Quality;
            string known;
            if (Defined(character).TryGetValue(identity.Instance, out known) && known == item)
            {
                return;
            }

            this.SendPlayerWeapon(character, weapon, placement, placement);
        }

        /// <summary>
        /// Forgets which weapons a character's client has been told about. A new
        /// session starts with a client that knows nothing, so call this before
        /// the login definitions go out.
        /// </summary>
        public static void BeginSession(ICharacter character)
        {
            Defined(character).Clear();
        }

        /// <summary>
        /// Per character: weapon identity instance, and the item it was last
        /// defined as for that character's client.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, string>> DefinedWeapons =
            new System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, string>>();

        private static System.Collections.Concurrent.ConcurrentDictionary<int, string> Defined(ICharacter character)
        {
            return DefinedWeapons.GetOrAdd(
                character.Identity.Instance,
                _ => new System.Collections.Concurrent.ConcurrentDictionary<int, string>());
        }

        private void SendPlayerWeapon(ICharacter character, IItem weapon, int placement, int location)
        {
            Defined(character)[PlayerWeaponIdentity(character, placement).Instance] =
                weapon.LowID + "/" + weapon.HighID + "/" + weapon.Quality;

            this.Send(
                character,
                message =>
                {
                    message.Identity = PlayerWeaponIdentity(character, placement);
                    message.Unknown = 0;
                    message.MsgVersion = Version;
                    message.Identitytype = (int)character.Identity.Type;
                    message.Instance = character.Identity.Instance;
                    // WeaponItemFullUpdate names the client-visible playfield
                    // instance, just like SimpleCharFullUpdate. Sending the
                    // static playfield number defines the weapon in a different
                    // world from its owner and the client does not draw it.
                    message.Playfield = Playfields.GetClientInstance(character.Playfield.Identity.Instance);
                    message.StateMachine = new Identity
                                           {
                                               Type = (IdentityType)StateMachineType,
                                               Instance = 0
                                           };

                    // The two bytes the old model packed into one wider field:
                    // the location passed in is an inventory id in the high
                    // byte and a body location in the low one.
                    message.InventoryId = (byte)((location >> 8) & 0xFF);
                    message.BodyLocation = (byte)(location & 0xFF);

                    message.Stats = Stats(
                        unchecked((uint)(weapon.GetAttribute(0) | 0x400)),
                        weapon.LowID,
                        weapon.Quality,
                        weapon.LowID,
                        weapon.HighID,
                        weapon.MultipleCount,
                        null,
                        null,
                        Optional(weapon.GetAttribute((int)StatIds.energy)));
                    message.Name = string.Empty;
                },
                false);
        }

        private static MessageDataFiller Filler(ICharacter wielder, DBMobSpawnWeapon weapon)
        {
            return message =>
            {
                message.Identity = new Identity
                                   {
                                       Type = (IdentityType)weapon.WeaponType,
                                       Instance = weapon.WeaponInstance
                                   };
                message.Unknown = 0;

                message.MsgVersion = Version;
                message.Identitytype = (int)wielder.Identity.Type;
                message.Instance = wielder.Identity.Instance;
                message.Playfield = Playfields.GetClientInstance(wielder.Playfield.Identity.Instance);
                message.StateMachine = new Identity
                                       {
                                           Type = (IdentityType)StateMachineType,
                                           Instance = 0
                                       };
                message.InventoryId = (byte)(weapon.InventoryId ?? Combat.CombatWeaponProfiles.DefaultInventoryId);
                message.BodyLocation = (byte)(weapon.BodyLocation ?? Combat.CombatWeaponProfiles.DefaultAttackSlot);

                message.Stats = Stats(
                    unchecked((uint)weapon.ItemFlags),
                    weapon.Unknown6,
                    weapon.QualityLevel,
                    weapon.ItemLowId,
                    weapon.ItemHighId,
                    weapon.Unknown7,
                    weapon.ItemDelay,
                    weapon.RechargeDelay,
                    weapon.Energy);
                message.Name = string.Empty;
            };
        }

        /// <summary>
        /// The common stats every captured weapon carries, followed by either
        /// or both timing stats when they were captured for that attachment.
        /// </summary>
        /// <remarks>
        /// The count in front of these used to be written by hand out of a
        /// database column, and for mob weapons that column says nine on some
        /// rows - which claimed nine stats and then sent seven. Deriving the
        /// count from the array is the fix.
        ///
        /// Captured player equipment uses the seven-stat shape. Captured spawned
        /// character attachments may add itemdelay and rechargedelay, which are
        /// retained in the normalized spawn row. Both shapes use this path; the
        /// caller supplies only fields established for its packet role.
        /// </remarks>
        private static GameTuple<CharacterStat, uint>[] Stats(
            uint itemFlags,
            int staticInstance,
            int quality,
            int templateLowId,
            int templateHighId,
            int multipleCount,
            int? itemDelay,
            int? rechargeDelay,
            int? energy)
        {
            var stats = new List<GameTuple<CharacterStat, uint>>
                        {
                            Stat(CharacterStat.Flags, itemFlags),
                            Stat((CharacterStat)StatStaticInstance, unchecked((uint)staticInstance)),
                            Stat((CharacterStat)StatAcgItemLevel, unchecked((uint)quality)),
                            Stat((CharacterStat)StatAcgItemTemplateId, unchecked((uint)templateLowId)),
                            Stat((CharacterStat)StatAcgItemTemplateId2, unchecked((uint)templateHighId)),
                            Stat((CharacterStat)StatMultipleCount, unchecked((uint)multipleCount)),
                            Stat((CharacterStat)StatEnergy, unchecked((uint)(energy ?? NoAmmo)))
                        };

            if (itemDelay.HasValue)
            {
                stats.Add(Stat((CharacterStat)StatIds.itemdelay, unchecked((uint)itemDelay.Value)));
            }

            if (rechargeDelay.HasValue)
            {
                stats.Add(Stat((CharacterStat)StatIds.rechargedelay, unchecked((uint)rechargeDelay.Value)));
            }

            return stats.ToArray();
        }

        private static int? Optional(int value)
        {
            return value == StatValue.Unset ? (int?)null : value;
        }

        private static GameTuple<CharacterStat, uint> Stat(CharacterStat id, uint value)
        {
            return new GameTuple<CharacterStat, uint> { Value1 = id, Value2 = value };
        }

        #endregion
    }
}
