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
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Playfields;

    #endregion

    /// <summary>
    /// Leaves a body where something died.
    /// </summary>
    /// <remarks>
    /// Killing something produced nothing at all before this - no corpse, so
    /// nothing to loot and nothing to show that the thing had died beyond it
    /// standing still at zero health.
    ///
    /// Everything here is read off the 154 corpses in the captured sessions.
    /// The values that are the same in all 154 are constants below. The ones
    /// that differ by creature are in the mobcorpses table, extracted from the
    /// same captures - the corpse model, how long it lies there, what is on it.
    ///
    /// Meshes are deliberately not sent. Eight of the 154 captured corpses set
    /// HasMeshes to zero and end the message there, so that form is known to be
    /// accepted by a real client, whereas inventing mesh names for a creature is
    /// not. A corpse renders from its CatMesh.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class CorpseFullUpdateMessageHandler :
        BaseMessageHandler<CorpseFullUpdateMessage, CorpseFullUpdateMessageHandler>
    {
        #region Constants

        /// <summary>
        /// The same in all 154 captured corpses.
        /// </summary>
        private const int Version = 8;

        private const int ItemVersionConstant = 11;

        private const short InventoryIdAndBodyLocationConstant = 111;

        private const int FlagsConstant = 1579013;

        private const int CorpseTypeConstant = 50000;

        private const int LockableVersionConstant = 2;

        private const int LockDifficultyConstant = 50;

        private const int ChestVersionConstant = 3;

        /// <summary>
        /// The game function of the one effect every captured corpse carries.
        /// </summary>
        private const int CorpseEffectFunction = 53031;

        private const int EffectVersion = 4;

        private const int EffectHits = 1;

        /// <summary>
        /// Seven arguments, four bytes each. The client's format for game
        /// function 53031 is what says seven.
        /// </summary>
        private const int ArgumentBytes = 28;

        /// <summary>
        /// The two fixed values inside the effect arguments, in the positions
        /// every captured corpse puts them.
        /// </summary>
        private static readonly int[] FixedArguments = { 1, 4 };

        #endregion

        #region Outbound

        /// <summary>
        /// Announces the corpse of a character that has just died.
        /// </summary>
        /// <returns>
        /// The corpse's identity, so that whatever wants to put loot in it can
        /// find it again.
        /// </returns>
        public Identity Send(ICharacter victim, Identity identity)
        {
            DBMobCorpse corpse = MobCorpseDao.Instance.GetWhere(new { MobName = victim.Name }).FirstOrDefault();

            this.Send(victim, Filler(victim, corpse, identity), true);
            return identity;
        }

        private static MessageDataFiller Filler(ICharacter victim, DBMobCorpse corpse, Identity identity)
        {
            return message =>
            {
                message.Identity = identity;
                message.Unknown = 0;

                message.MsgVersion = Version;
                message.ItemVersion = ItemVersionConstant;
                message.HolderType = 0;
                message.HolderInstance = 0;

                OmniCell.Core.Vector.Coordinate where = victim.Coordinates();
                message.Coordinates = new Vector3 { X = where.x, Y = where.y, Z = where.z };

                OmniCell.Core.Vector.Quaternion heading = victim.Heading;
                message.Heading = new Quaternion
                                  {
                                      X = heading.xf,
                                      Y = heading.yf,
                                      Z = heading.zf,
                                      W = heading.wf
                                  };

                // The instance, like everything else the client is told about
                // this playfield. See PlayfieldAnarchyFMessageHandler.
                message.PlayfieldId = Playfields.GetClientInstance(victim.Playfield.Identity.Instance);
                message.StateMachineType = 0;
                message.StateMachineInstance = 0;
                message.InventoryIdAndBodyLocation = InventoryIdAndBodyLocationConstant;

                message.Stats = Stats(victim, corpse).ToArray();
                message.Name = "Remains of " + victim.Name;

                message.LockableVersion = LockableVersionConstant;
                message.LockDifficulty = LockDifficultyConstant;
                message.Keyholders = new Identity[0];
                message.ChestVersion = ChestVersionConstant;
                message.NanoEffects = new[] { Effect(victim, corpse) };
                message.Owner = victim.Identity;

                // Five places, ids all zero in every captured corpse.
                message.Textures =
                    Enumerable.Range(0, 5).Select(i => new Texture { Place = i, Id = 0, Group = 0 }).ToArray();

                message.HasMeshes = 0;
                message.Meshes = new CorpseMesh[0];
            };
        }

        /// <summary>
        /// The stat list, in the order the live server writes it.
        /// </summary>
        /// <remarks>
        /// Order matters - this is a list, not a map, and the client reads it in
        /// sequence. Head mesh is last and only present when there is one, which
        /// is what the two humanoid corpses in the captures do.
        /// </remarks>
        private static IEnumerable<GameTuple<int, int>> Stats(ICharacter victim, DBMobCorpse corpse)
        {
            var stats = new List<GameTuple<int, int>>
                        {
                            Stat(StatIds.flags, FlagsConstant),
                            Stat(StatIds.staticinstance, 0),
                            Stat(StatIds.acgitemlevel, 0),
                            Stat(StatIds.acgitemtemplateid, 0),
                            Stat(StatIds.acgitemtemplateid2, 0),
                            Stat(StatIds.multiplecount, 1),
                            Stat(StatIds.monsterscale, corpse == null ? 100 : corpse.MonsterScale),
                            Stat(StatIds.canchangeclothes, corpse == null ? 0 : corpse.CanChangeClothes),
                            Stat(StatIds.sex, corpse == null ? victim.Stats[StatIds.sex].Value : corpse.Sex),
                            Stat(StatIds.breed, corpse == null ? victim.Stats[StatIds.breed].Value : corpse.Breed),
                            Stat(StatIds.race, corpse == null ? victim.Stats[StatIds.race].Value : corpse.Race),
                            Stat(StatIds.corpsetype, CorpseTypeConstant),
                            Stat(StatIds.corpseinstance, victim.Identity.Instance),
                            Stat(StatIds.catmesh, corpse == null ? 0 : corpse.CatMesh),
                            Stat(StatIds.cash, corpse == null ? 0 : corpse.Cash),
                            Stat(StatIds.timeexist, corpse == null ? 0 : corpse.TimeExist),
                            Stat(StatIds.deadtimer, corpse == null ? 60 : corpse.DeadTimer)
                        };

            if (corpse != null && corpse.HeadMesh != 0)
            {
                stats.Add(Stat(StatIds.headmesh, corpse.HeadMesh));
            }

            return stats;
        }

        private static GameTuple<int, int> Stat(StatIds stat, int value)
        {
            return new GameTuple<int, int> { Value1 = (int)stat, Value2 = value };
        }

        /// <summary>
        /// The one effect a corpse carries.
        /// </summary>
        /// <remarks>
        /// This used to be eight separate fields on the message, because the
        /// record was modelled flat. It is one GameData SpellData_t, the same
        /// record SpellList and ApplySpells carry: an identity whose type half
        /// is the game function, a version, no criteria, the four integers every
        /// effect has, and then the function's own arguments.
        ///
        /// Game function 53031 takes seven of them, and the client says so -
        /// its format is SpellStats 5, 6, 7, 45, 47, 48 and 11, which is where
        /// the twenty eight bytes come from. Two of the seven vary per corpse
        /// and come out of the database; two are fixed at 1 and 4 in every
        /// captured copy; the other three are zero.
        /// </remarks>
        private static NanoEffect Effect(ICharacter victim, DBMobCorpse corpse)
        {
            var arguments = new byte[ArgumentBytes];
            WriteInt32(arguments, 0, 0);
            WriteInt32(arguments, 4, 0);
            WriteInt32(arguments, 8, corpse == null ? 0 : corpse.Unknown20);
            WriteInt32(arguments, 12, FixedArguments[0]);
            WriteInt32(arguments, 16, FixedArguments[1]);
            WriteInt32(arguments, 20, corpse == null ? 0 : corpse.Unknown23);
            WriteInt32(arguments, 24, 0);

            return new NanoEffect
                       {
                           Effect = new Identity
                                    {
                                        Type = (IdentityType)CorpseEffectFunction,
                                        Instance = victim.Identity.Instance
                                    },
                           Version = EffectVersion,
                           CriterionCount = 0,
                           Criteria = new NanoCriterion[0],
                           Hits = EffectHits,
                           Amount = 0,
                           Target = NanoEffectTarget.None,
                           SpellList = 0,
                           Arguments = arguments
                       };
        }

        /// <summary>
        /// Big-endian, like everything else on this wire.
        /// </summary>
        private static void WriteInt32(byte[] buffer, int at, int value)
        {
            buffer[at] = (byte)(value >> 24);
            buffer[at + 1] = (byte)(value >> 16);
            buffer[at + 2] = (byte)(value >> 8);
            buffer[at + 3] = (byte)value;
        }

        #endregion
    }
}
