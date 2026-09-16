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

namespace OmniCell.Core.Playfields
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Events;
    using OmniCell.Core.Functions;
    using OmniCell.Core.Items;
    using OmniCell.Core.Network;
    using OmniCell.Core.NPCHandler;
    using OmniCell.Core.Statels;
    using OmniCell.Core.Vector;
    using OmniCell.Core.VendorHandler;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;
    using OmniCell.Interfaces;
    using OmniCell.ObjectManager;
    using OmniCell.Stats.SpecialStats;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.SystemMessages;

    using Utility;

    using ZoneEngine.Core;
    using ZoneEngine.Core.Combat;
    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.Functions;
    using ZoneEngine.Core.InternalMessages;
    using ZoneEngine.Core.KnuBot;
    using ZoneEngine.Core.Loot;
    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Quests;
    using ZoneEngine.Core.Packets;
    using ZoneEngine.Core.Playfields;
    using ZoneEngine.Script;

    using Config = Utility.Config.ConfigReadWrite;
    using Quaternion = SmokeLounge.AOtomation.Messaging.GameData.Quaternion;
    using Vector3 = SmokeLounge.AOtomation.Messaging.GameData.Vector3;

    #endregion

    /// <summary>
    /// </summary>
    public class Playfield : PooledObject, IPlayfield
    {
        #region Fields

        private readonly List<StaticDynel> staticDynels = new List<StaticDynel>();

        /// <summary>
        /// </summary>
        private readonly OmniCell.Core.Components.DisposeContainer memBusDisposeContainer =
            new OmniCell.Core.Components.DisposeContainer();

        /// <summary>
        /// Everything sent through this playfield, delivered in the order it was published.
        /// </summary>
        private readonly OmniCell.Core.Components.MessageBus playfieldBus;

        /// <summary>
        /// </summary>
        private readonly ZoneServer server;

        /// <summary>
        /// </summary>
        private List<PlayfieldDistrict> districts = new List<PlayfieldDistrict>();

        /// <summary>
        /// </summary>
        private readonly Timer heartBeat;

        /// <summary>
        /// </summary>
        private readonly List<StatelData> statels = new List<StatelData>();

        /// <summary>
        /// </summary>
        private float x;

        private bool disposed = false;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        /// <param name="zoneServer">
        /// </param>
        /// <param name="playfieldIdentity">
        /// </param>
        public Playfield(ZoneServer zoneServer, Identity playfieldIdentity)
            : base(Identity.None, playfieldIdentity)
        {
            this.server = zoneServer;
            this.playfieldBus = new OmniCell.Core.Components.MessageBus();

            this.memBusDisposeContainer.Add(
                this.playfieldBus.Subscribe<IMSendAOtomationMessageToClient>(SendAOtomationMessageToClient));
            this.memBusDisposeContainer.Add(
                this.playfieldBus.Subscribe<IMSendAOtomationMessageToPlayfield>(this.SendAOtomationMessageToPlayfield));
            this.memBusDisposeContainer.Add(
                this.playfieldBus.Subscribe<IMSendAOtomationMessageToPlayfieldOthers>(
                    this.SendAOtomationMessageToPlayfieldOthers));
            this.memBusDisposeContainer.Add(
                this.playfieldBus.Subscribe<IMSendAOtomationMessageBodyToClient>(this.SendAOtomationMessageBodyToClient));
            this.memBusDisposeContainer.Add(
                this.playfieldBus.Subscribe<IMSendAOtomationMessageBodiesToClient>(
                    this.SendAOtomationMessageBodiesToClient));
            this.memBusDisposeContainer.Add(this.playfieldBus.Subscribe<IMSendPlayerSCFUs>(this.SendSCFUsToClient));
            this.memBusDisposeContainer.Add(this.playfieldBus.Subscribe<IMExecuteFunction>(this.ExecuteFunction));
            this.heartBeat = new Timer(this.HeartBeatTimer, null, HeartBeatMilliseconds, 0);

            this.statels = PlayfieldLoader.PFData[this.Identity.Instance].Statels;
            this.LoadMobSpawnWeapons(playfieldIdentity);
            this.LoadMobSpawnMeshes(playfieldIdentity);
            this.LoadMobSpawns(playfieldIdentity);
            this.LoadVendors(playfieldIdentity);
            this.LoadStaticDynels(playfieldIdentity);
        }

        /// <summary>
        /// Weapons held by the characters spawned in this playfield, by spawn id.
        /// </summary>
        /// <remarks>
        /// Kept here rather than on the character because a spawned mob has no
        /// inventory to put a weapon in. This is the display only - what the
        /// client needs to draw the thing in its hands - and it is read once
        /// when the playfield loads rather than per client.
        /// </remarks>
        private readonly Dictionary<int, List<DBMobSpawnWeapon>> spawnWeapons =
            new Dictionary<int, List<DBMobSpawnWeapon>>();

        /// <summary>
        /// The weapons a character is holding, if any.
        /// </summary>
        public IEnumerable<DBMobSpawnWeapon> WeaponsOf(Identity character)
        {
            List<DBMobSpawnWeapon> weapons;
            return this.spawnWeapons.TryGetValue(character.Instance, out weapons)
                       ? weapons
                       : new List<DBMobSpawnWeapon>();
        }

        /// <summary>
        /// When each player was last sent a keepalive.
        /// </summary>
        private readonly Dictionary<Identity, DateTime> lastPing = new Dictionary<Identity, DateTime>();

        /// <summary>
        /// Which characters each client has been introduced to.
        /// </summary>
        /// <remarks>
        /// This is the whole of what a client believes about the playfield. A
        /// client can only be told about a character it has been introduced to -
        /// a stat update, a movement, a swing, a death - because until the
        /// SimpleCharFullUpdate arrives the identity means nothing to it, and
        /// afterwards it means the wrong thing if the client was told to forget.
        ///
        /// Keyed by the player, holding the characters. Written by the streaming
        /// pass on the heartbeat thread and read by Announce on whichever thread
        /// happens to be sending, so every touch is under the lock.
        /// </remarks>
        private readonly Dictionary<Identity, HashSet<Identity>> introduced =
            new Dictionary<Identity, HashSet<Identity>>();

        /// <summary>
        /// The lock over introduced.
        /// </summary>
        private readonly object introducedLock = new object();

        /// <summary>
        /// When the next streaming pass is due.
        /// </summary>
        private DateTime nextStream = DateTime.MinValue;

        /// <summary>
        /// Where each spawned character belongs.
        /// </summary>
        /// <remarks>
        /// A row in mobspawns is a spawn point, not a character. The character
        /// it makes can walk off, fight, and be killed; the point stays where it
        /// is and makes another. This is that point - the coordinates and facing
        /// the row was read with, kept because the character will have wandered
        /// away from them by the time it is needed.
        ///
        /// Only spawned characters are in here. A player has no spawn point, and
        /// nothing in this file should bring a player back from the dead.
        /// </remarks>
        private readonly Dictionary<Identity, Coordinate> spawnPoint =
            new Dictionary<Identity, Coordinate>();

        /// <summary>
        /// Which way each spawn point faces.
        /// </summary>
        private readonly Dictionary<Identity, OmniCell.Core.Vector.Quaternion> spawnHeading =
            new Dictionary<Identity, OmniCell.Core.Vector.Quaternion>();

        /// <summary>
        /// The health each spawn point makes its character with, where that is less than its most.
        /// </summary>
        /// <remarks>
        /// The Wounded Dockworkers of Arete Landing are 32 health with 20 of it missing, and they stay
        /// that way: the live server tells everyone nearby, once a second, that each of them is on 12
        /// (20260914-124401 s4, 12:47:49 to 12:48:19). Regeneration brings a character back up to this
        /// and no further. Characters that spawn whole are not in here and heal all the way.
        /// </remarks>
        private readonly Dictionary<Identity, int> spawnHealth = new Dictionary<Identity, int>();

        /// <summary>
        /// When each body should be taken away, and when its spawn point should
        /// produce another.
        /// </summary>
        private readonly Dictionary<Identity, DateTime> bodyGoesAt =
            new Dictionary<Identity, DateTime>();

        private readonly Dictionary<Identity, DateTime> risesAt = new Dictionary<Identity, DateTime>();

        /// <summary>
        /// The lock over the two above.
        /// </summary>
        private readonly object deadLock = new object();

        /// <summary>
        /// How far away a character can be and still be worth hearing about.
        /// </summary>
        /// <remarks>
        /// Measured, not picked. Walking into Arete Landing, the live server
        /// introduced fifty six characters and no more: the nearest at zero, the
        /// median at nineteen, and the furthest at sixty two and a half units.
        /// Four hundred and eighty three characters stand in that playfield, so
        /// the other four hundred and twenty seven were simply not this player's
        /// business yet.
        ///
        /// Use the observed boundary itself. Adding a margin here introduces
        /// characters the captured server did not introduce, which turns a
        /// measurement back into a guess and makes entry failures harder to
        /// isolate.
        ///
        /// The playfield is only three hundred and fifty units across, which is
        /// why the first guess at this - three hundred - filtered out nineteen
        /// characters out of four hundred and eighty three and saved nothing.
        /// </remarks>
        private const float VicinityRadius = 62.5f;

        /// <summary>
        /// How far away a character has to be before a client is told to forget
        /// it again.
        /// </summary>
        /// <remarks>
        /// Both boundaries were looked for, in five Arete Landing sessions, by matching every
        /// SimpleCharFullUpdate and every Despawn against where the player was
        /// standing at that moment: the two distributions come out on top of one
        /// another - a median of sixty nine to introduce and sixty five to
        /// remove - which is one radius, not two.
        ///
        /// The live server has no hysteresis. In one session it
        /// introduced two hundred and eleven characters for the first time and
        /// re-introduced six hundred and eighty three, having removed eight
        /// hundred and eighty six: characters sitting on the boundary going in
        /// and out with every step the player took, three packets of flicker for
        /// every one that said something. Until the first zone is stable, copy
        /// that observed single boundary rather than adding an unmeasured one.
        /// </remarks>
        private const float ForgetRadius = VicinityRadius;

        /// <summary>
        /// How long between passes over who can see whom.
        /// </summary>
        /// <remarks>
        /// A quarter second. This costs one distance check per player per
        /// character in the playfield, which is the one part of the heartbeat
        /// that grows with the number of players rather than being shared
        /// between them, so it does not want doing at the full heartbeat rate.
        ///
        /// A quarter second is about four units of running.
        /// </remarks>
        private const int StreamMilliseconds = 250;

        /// <summary>
        /// How long a killed character lies where it fell before the client is
        /// told to take it away.
        /// </summary>
        /// <remarks>
        /// Ten seconds. Measured: across the captures, thirty eight bodies could
        /// be matched from the corpse arriving to the character being despawned,
        /// and ten of them land between nine point three and nine point seven
        /// seconds. The long tail - a minute, three minutes, ten minutes - is
        /// not the body lingering, it is the player having walked away, which
        /// despawns things for an entirely different reason.
        ///
        /// The corpse is not this. The corpse is a dynel of its own, put there
        /// by the killing blow so there is something to loot, and it outlives
        /// the body.
        /// </remarks>
        private const int CorpseLingerSeconds = 10;

        /// <summary>
        /// How long after a death the spawn point produces another one.
        /// </summary>
        /// <remarks>
        /// Thirty seconds, and this one is as clean as measurement gets here.
        /// Thirteen deaths in the captures could be followed to the next arrival
        /// of the same kind of character within three units of where the last
        /// one fell, and seven of the thirteen land between twenty nine point
        /// three and twenty nine point eight seconds. The six that do not are
        /// all longer, which is what you would expect: a respawn is only seen
        /// when somebody is there to be told about it.
        /// </remarks>
        private const int RespawnSeconds = 30;

        /// <summary>
        /// How long the playfield waits between passes over everything in it.
        /// </summary>
        /// <remarks>
        /// It was ten, which is the rate packets arrive at, not the rate a world
        /// needs thinking about. Every pass walks every character in the
        /// playfield and asks it about regeneration, its fight, its nano, its
        /// experience and where it has got to - a hundred times a second, for
        /// four hundred and eighty three characters.
        ///
        /// Nothing here needs that. Regeneration is measured in seconds, a swing
        /// takes about one, a nano takes several, and a character walking to a
        /// waypoint is drawn by the client from the destination it was given -
        /// the server only has to notice the arrival. A tenth of a second is
        /// still far finer than any of those and costs a tenth as much.
        /// </remarks>
        private const int HeartBeatMilliseconds = 100;

        /// <summary>
        /// How often the live server pings a connected client.
        /// </summary>
        /// <remarks>
        /// Measured rather than picked: three captured sessions carry 27 pings
        /// in 1086 seconds, 35 in 1292, and 23 in 957 - 40.2, 36.9 and 41.6
        /// seconds apart. Forty is the round number those sit around.
        ///
        /// OmniCell sent none at all. A client that expects to hear from the
        /// server on a schedule and does not is a client that eventually decides
        /// the server is gone.
        /// </remarks>
        private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(40);

        /// <summary>
        /// Sends the keepalive if this player is due one.
        /// </summary>
        private void Keepalive(ICharacter dynel)
        {
            var client = dynel.Controller.Client as ZoneClient;
            if (client == null)
            {
                return;
            }

            DateTime last;
            if (!this.lastPing.TryGetValue(dynel.Identity, out last))
            {
                // A character that has just arrived is not overdue a ping. The
                // first heartbeat after it entered used to send one straight
                // away, which put a ping in the entry stream where the captures
                // have none - the live server's first is forty seconds later,
                // like all the rest.
                this.lastPing[dynel.Identity] = DateTime.UtcNow;
                return;
            }

            if (DateTime.UtcNow - last < PingInterval)
            {
                return;
            }

            this.lastPing[dynel.Identity] = DateTime.UtcNow;

            // A request, with the stamp the client will quote back. The two
            // later stamps stay zero because nothing has answered it yet -
            // that is what separates a request from an echo on the wire, and
            // every one of the 63 captured requests looks like this.
            //
            // Until 2026-09-10 this went out as a bare header, sixteen bytes
            // where the live server sends forty, because the message had no
            // fields at all.
            client.SendCompressed(
                new PingMessage
                    {
                        PingObjType = PingMessage.Request,
                        HopCount = 0,
                        OriginatorStamp = Uptime(),
                        ReceiveStamp = 0,
                        TransmitStamp = 0,
                        Sequence = 0
                    });
        }

        /// <summary>
        /// Milliseconds since this process started.
        /// </summary>
        /// <remarks>
        /// The units the captured pings use: the originator stamp advances
        /// about 30,274 between pings thirty seconds apart. The epoch does not
        /// have to match the live server's - nothing compares one server's
        /// stamps with another's - but the rate does, because the far end
        /// subtracts two of them to get a round trip.
        /// </remarks>
        private static int Uptime()
        {
            // Through long first: casting a double straight to int does not wrap even when unchecked -
            // past 24.8 days it gave int.MinValue on .NET Framework and gives int.MaxValue on .NET 10,
            // and the stamp stopped moving. The long keeps counting and its low 32 bits wrap.
            return unchecked((int)(long)(DateTime.UtcNow - Started).TotalMilliseconds);
        }

        /// <summary>
        /// When this process started, for Uptime.
        /// </summary>
        private static readonly DateTime Started = DateTime.UtcNow;

        /// <summary>
        /// The doors this playfield holds.
        /// </summary>
        /// <remarks>
        /// Read out of the statel data the playfield was built from, which is
        /// the same place the door functions come from, so a door that can be
        /// walked through is a door that gets reported.
        /// </remarks>
        public IEnumerable<Identity> Doors()
        {
            return this.statels
                .Where(s => s.Identity.Type == IdentityType.Door)
                .Select(s => new Identity { Type = IdentityType.Door, Instance = s.Identity.Instance })
                .ToList();
        }

        /// <summary>
        /// The meshes of the characters spawned in this playfield, by spawn id.
        /// </summary>
        /// <remarks>
        /// Kept here for the same reason as the weapons: a spawned character has
        /// no inventory to build a mesh list out of, and this is read once when
        /// the playfield loads rather than per client.
        /// </remarks>
        private readonly Dictionary<int, List<DBMobSpawnMesh>> spawnMeshes =
            new Dictionary<int, List<DBMobSpawnMesh>>();

        /// <summary>
        /// The meshes a spawned character is drawn from, if any are known.
        /// </summary>
        public IEnumerable<DBMobSpawnMesh> MeshesOf(Identity character)
        {
            List<DBMobSpawnMesh> meshes;
            return this.spawnMeshes.TryGetValue(character.Instance, out meshes)
                       ? meshes
                       : new List<DBMobSpawnMesh>();
        }

        private void LoadMobSpawnMeshes(Identity playfieldIdentity)
        {
            this.spawnMeshes.Clear();
            foreach (DBMobSpawnMesh mesh in
                MobSpawnMeshDao.Instance.GetWhere(new { Playfield = playfieldIdentity.Instance }))
            {
                if (!this.spawnMeshes.ContainsKey(mesh.Id))
                {
                    this.spawnMeshes[mesh.Id] = new List<DBMobSpawnMesh>();
                }

                this.spawnMeshes[mesh.Id].Add(mesh);
            }
        }

        private void LoadMobSpawnWeapons(Identity playfieldIdentity)
        {
            this.spawnWeapons.Clear();
            foreach (DBMobSpawnWeapon weapon in
                MobSpawnWeaponDao.Instance.GetWhere(new { Playfield = playfieldIdentity.Instance }))
            {
                if (!this.spawnWeapons.ContainsKey(weapon.SpawnId))
                {
                    this.spawnWeapons[weapon.SpawnId] = new List<DBMobSpawnWeapon>();
                }

                this.spawnWeapons[weapon.SpawnId].Add(weapon);
            }
        }

        private void LoadStaticDynels(Identity playfieldIdentity)
        {
            IEnumerable<DBStaticDynel> dynels =
                StaticDynelDao.Instance.GetWhere(new { Playfield = playfieldIdentity.Instance });
            foreach (DBStaticDynel sd in dynels)
            {
                List<GameTuple<CharacterStat, uint>> tempStats =
                    MessagePackZip.DeserializeData<GameTuple<CharacterStat, uint>>(sd.stats);

                if (tempStats.Any(x => x.Value1 == (CharacterStat)StatIds.acgitemtemplateid))
                {
                    int id = (int)tempStats.First(x => x.Value1 == (CharacterStat)StatIds.acgitemtemplateid).Value2;
                    StaticDynel sdy = new StaticDynel(
                        this.Identity,
                        new Identity() { Type = (IdentityType)sd.Type, Instance = sd.Instance },
                        ItemLoader.ItemList[id]);

                    // The database list is the SimpleItemFullUpdate wire list,
                    // not a partial override of every stat on the item template.
                    // Retail Gas Fire packets contain exactly these eight rows;
                    // merging template defaults made the local packet contain 15.
                    // Events and actions remain on Template and are unaffected.
                    sdy.Stats.Clear();
                    foreach (GameTuple<CharacterStat, uint> stat in tempStats)
                    {
                        sdy.WireStats.Add(
                            new GameTuple<CharacterStat, uint> { Value1 = stat.Value1, Value2 = stat.Value2 });
                        if (sdy.Stats.ContainsKey((int)stat.Value1))
                        {
                            sdy.Stats[(int)stat.Value1] = (int)stat.Value2;
                            continue;
                        }
                        sdy.Stats.Add((int)stat.Value1, (int)stat.Value2);
                    }

                    sdy.Coordinate = new Coordinate(sd.X, sd.Y, sd.Z);
                    sdy.Heading = new Quaternion()
                                  {
                                      X = sd.HeadingX,
                                      Y = sd.HeadingY,
                                      Z = sd.HeadingZ,
                                      W = sd.HeadingW
                                  };
                }
            }
        }

        private void LoadVendors(Identity playfieldIdentity)
        {
            VendorHandler.SpawnVendorsForPlayfield(
                this,
                PlayfieldLoader.PFData[playfieldIdentity.Instance].Statels.Where(
                    x => x.Identity.Type == IdentityType.VendingMachine).ToArray());
        }

        private void LoadMobSpawns(Identity playfieldIdentity)
        {
            IEnumerable<DBMobSpawn> mobs = MobSpawnDao.Instance.GetWhere(new { Playfield = playfieldIdentity.Instance });

            // Conversations extracted from captures, by character name - see
            // Documentation/Quest-System.md. Read once for the playfield rather than per spawn.
            ILookup<string, DBKnuBotDialogue> conversations = KnuBotDialogueDao.Instance
                .GetWhere(new { Playfield = playfieldIdentity.Instance })
                .ToLookup(d => d.NpcName, StringComparer.Ordinal);
            ILookup<string, DBKnuBotOpener> openers = KnuBotOpenerDao.Instance
                .GetWhere(new { Playfield = playfieldIdentity.Instance })
                .ToLookup(o => o.NpcName, StringComparer.Ordinal);

            foreach (DBMobSpawn mob in mobs)
            {
                IEnumerable<DBMobSpawnStat> stats = MobSpawnStatDao.Instance.GetWhere(new { mob.Id, mob.Playfield });
                ICharacter cmob = NonPlayerCharacterHandler.InstantiateMobSpawn(
                    mob,
                    stats.ToArray(),
                    new NPCController(),
                    this);

                // Where this one belongs, so it can be put back there. See
                // spawnPoint.
                if (cmob != null)
                {
                    this.spawnPoint[cmob.Identity] = new Coordinate { x = mob.X, y = mob.Y, z = mob.Z };
                    this.spawnHeading[cmob.Identity] = new OmniCell.Core.Vector.Quaternion(
                        mob.HeadingX,
                        mob.HeadingY,
                        mob.HeadingZ,
                        mob.HeadingW);

                    int health = cmob.Stats[StatIds.health].Value;
                    if (health > 0 && health < cmob.Stats[StatIds.life].Value)
                    {
                        lock (this.spawnHealth)
                        {
                            this.spawnHealth[cmob.Identity] = health;
                        }
                    }
                }

                // What the live server had this character say, if a capture
                // heard it - or what somebody wrote with /questedit.
                //
                // Read before anything is decided, because having a
                // conversation is on its own reason enough to be able to hold
                // one. A character written in the game gets its words before it
                // gets its quest, and there is nothing to be gained by leaving
                // it mute in between.
                var script = cmob == null
                                 ? new List<DBKnuBotScript>()
                                 : KnuBotScriptDao.Instance
                                       .GetWhere(new { Npc = mob.Id, Playfield = mob.Playfield })
                                       .ToList();

                // Otherwise, a character a quest has anything to do with can be
                // talked to.
                //
                // Giving one out is the obvious case, and it is what this used
                // to check. It is not the only one: twenty eight of Arete
                // Landing's quests ask you to go and speak to somebody, and
                // every one of those was unapproachable because nothing had
                // given them a conversation. Rex Larsson sends you to Stan
                // Goodman and Stan Goodman had nothing to say.
                if (cmob != null && conversations[mob.Name].Any())
                {
                    ((NPCController)cmob.Controller).SetKnuBot(
                        new ConversationKnuBot(cmob.Identity, mob.Name, conversations[mob.Name], openers[mob.Name]));
                }
                else if (script.Count > 0)
                {
                    ((NPCController)cmob.Controller).SetKnuBot(new ScriptedKnuBot(cmob.Identity, script));
                }
                else if (cmob != null
                         && (QuestManager.All().Any(q => q.GiverId == mob.Id) || QuestManager.IsSpokenTo(mob.Name)))
                {
                    // The built-in one, which is plainly ours.
                    ((NPCController)cmob.Controller).SetKnuBot(new QuestGiverKnuBot(cmob.Identity));
                }
                if (mob.KnuBotScriptName != "")
                {
                    ((NPCController)cmob.Controller).SetKnuBot(
                        ScriptCompiler.Instance.CreateKnuBot(mob.KnuBotScriptName, cmob.Identity));

                    /*                    if ((cmob.Stats[0].Value
                        & (int)SimpleCharFullUpdateFlags.IsImmune) == (int)SimpleCharFullUpdateFlags.IsImmune)
                    {
                        cmob.Stats[0].Value -= (int)SimpleCharFullUpdateFlags.IsImmune;
                        cmob.Stats[0].Value |= (int)SimpleCharFullUpdateFlags.UnknownFlag5;
                    }*/
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// </summary>
        public List<PlayfieldDistrict> Districts
        {
            get
            {
                return this.districts;
            }

            private set
            {
                this.districts = value;
            }
        }

        /// <summary>
        /// </summary>
        public List<Function> EnvironmentFunctions { get; private set; }

        /// <summary>
        /// </summary>
        public Expansions Expansion { get; set; }

        /// <summary>
        /// </summary>
        public float X
        {
            get
            {
                return this.X;
            }

            set
            {
                this.x = value;
            }
        }

        /// <summary>
        /// </summary>
        public float XScale { get; set; }

        /// <summary>
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// </summary>
        public float ZScale { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public void Announce(Message message)
        {
            Announce(message.Body);
        }

        /// <summary>
        /// </summary>
        /// <param name="messageBody">
        /// </param>
        public void Announce(MessageBody messageBody)
        {
            // Where the thing this is about is standing, if it is about anything
            // in particular. Looked up once, not once per listener.
            Coordinate origin;
            Identity subject;
            bool located = this.Subject(messageBody, out subject, out origin);

            foreach (Character entity in
                Pool.Instance.GetAll<Character>((int)IdentityType.CanbeAffected)
                    .Where(x => x.InPlayfield(this.Identity)))
            {
                if (entity != null)
                {
                    // Make this whole thing unblocking with publishing single internal messages
                    if (entity.Controller.Client != null && !Deaf(entity, messageBody)
                        && (!located || entity.Identity == subject || this.Knows(entity.Identity, subject)))
                    {
                        this.Publish(
                            new IMSendAOtomationMessageBodyToClient()
                            {
                                client = entity.Controller.Client,
                                Body = messageBody
                            });
                    }
                }
            }
        }

        /// <summary>
        /// Who or what a message is about, and where they are.
        /// </summary>
        /// <remarks>
        /// False for anything that is not about one character in this playfield -
        /// the tower and city lists, the weather, a message whose subject has
        /// already gone. Those go to everybody, as they should.
        /// </remarks>
        private bool Subject(MessageBody messageBody, out Identity subject, out Coordinate origin)
        {
            subject = Identity.None;
            origin = new Coordinate();

            var n3 = messageBody as N3Message;
            if (n3 == null || n3.Identity.Type != IdentityType.CanbeAffected)
            {
                return false;
            }

            var character = this.FindByIdentity<ICharacter>(n3.Identity);
            if (character == null)
            {
                return false;
            }

            subject = n3.Identity;
            origin = new Coordinate(character.RawCoordinates);
            return true;
        }

        /// <summary>
        /// Whether a character is close enough to be told.
        /// </summary>
        /// <remarks>
        /// A playfield is not a room. Sending every message about every
        /// character to every player is fine while there is one player and
        /// nothing moves, and stops being fine immediately after that: a hundred
        /// characters walking around, told to twenty players, is two thousand
        /// messages for every step any of them takes, and it grows with the
        /// product rather than the sum.
        ///
        /// So a message about somebody goes to the people who could see them.
        /// The subject always hears about itself, whatever the distance, because
        /// some of what is announced about a character is the character being
        /// told something.
        ///
        /// The radius is measured - see VicinityRadius. This is what decides who
        /// gets introduced to whom; once introduced, Knows is what decides who
        /// hears about whom, because a client that has been told about a
        /// character has to keep being told until it is told to forget.
        /// </remarks>
        private bool Nearby(ICharacter listener, Coordinate origin)
        {
            return new Coordinate(listener.RawCoordinates).Distance2D(origin) <= VicinityRadius;
        }

        /// <summary>
        /// Whether a spawned character is dead.
        /// </summary>
        /// <remarks>
        /// A player at zero health is not this. A player that dies is still a
        /// character in the playfield and everybody should keep being told about
        /// it; a spawn at zero health is a body on its way out.
        /// </remarks>
        private static bool Fallen(ICharacter character)
        {
            return character.Controller.Client == null && character.Stats[StatIds.health].Value <= 0;
        }

        /// <summary>
        /// Whether a client has been introduced to a character.
        /// </summary>
        private bool Knows(Identity listener, Identity subject)
        {
            lock (this.introducedLock)
            {
                HashSet<Identity> known;
                return this.introduced.TryGetValue(listener, out known) && known.Contains(subject);
            }
        }

        /// <summary>
        /// The set of characters a client has been introduced to, made if needed.
        /// </summary>
        private HashSet<Identity> Known(Identity listener)
        {
            lock (this.introducedLock)
            {
                HashSet<Identity> known;
                if (!this.introduced.TryGetValue(listener, out known))
                {
                    known = new HashSet<Identity>();
                    this.introduced[listener] = known;
                }

                return known;
            }
        }

        /// <summary>
        /// Tell one client about one character.
        /// </summary>
        /// <remarks>
        /// The character, then the thing in its hands, in that order - the order
        /// the live server uses, and without it every armed character is drawn
        /// empty-handed.
        ///
        /// No CharInPlay. That is not "here is a character": the live server
        /// sends exactly one per session, for the player, at the end of entry,
        /// and the client answers it to say it has finished loading.
        /// </remarks>
        private void Introduce(IZoneClient client, Character subject)
        {
            client.SendCompressed(SimpleCharFullUpdate.ConstructMessage(subject));

            foreach (DBMobSpawnWeapon weapon in this.WeaponsOf(subject.Identity))
            {
                WeaponItemFullUpdateMessageHandler.Default.Send(
                    client.Controller.Character,
                    subject,
                    weapon);
            }
        }

        /// <summary>
        /// Introduce every player to what has come near it and take away what has
        /// gone.
        /// </summary>
        /// <remarks>
        /// A playfield holds four hundred and eighty three characters and a
        /// client can see perhaps fifty of them. Sending all four hundred and
        /// eighty three on entry - which is what this did - is a thousand
        /// packets before the player has moved, and then every one of those
        /// characters reports its regeneration and its walking and its fighting
        /// to a client that cannot see it, forever.
        ///
        /// The live server sends between twenty six and forty nine on entry,
        /// across the sessions captured, and introduces and removes the rest as
        /// the player walks. This is that.
        ///
        /// One pass is one distance check per player per character. Everything
        /// else in the heartbeat is shared between players; this is not, so it
        /// runs on its own slower clock - see StreamMilliseconds.
        /// </remarks>
        private void Stream()
        {
            if (DateTime.UtcNow < this.nextStream)
            {
                return;
            }

            this.nextStream = DateTime.UtcNow + TimeSpan.FromMilliseconds(StreamMilliseconds);

            List<Character> here =
                Pool.Instance.GetAll<Character>((int)IdentityType.CanbeAffected)
                    .Where(x => x != null && x.InPlayfield(this.Identity))
                    .ToList();

            foreach (Character player in here)
            {
                // A character with no client is a spawn, and there are several
                // hundred of them. A character still entering the world has not
                // been introduced to anybody yet and ClientConnected is in the
                // middle of doing it.
                if (player.Controller.Client == null || player.EnteringWorld)
                {
                    continue;
                }

                HashSet<Identity> known = this.Known(player.Identity);
                var from = new Coordinate(player.RawCoordinates);

                foreach (Character subject in here)
                {
                    if (subject.Identity == player.Identity)
                    {
                        continue;
                    }

                    double distance = from.Distance2D(new Coordinate(subject.RawCoordinates));

                    bool knows;
                    lock (this.introducedLock)
                    {
                        knows = known.Contains(subject.Identity);
                    }

                    // A body waiting to be taken away is not introduced to
                    // anybody new. It is already gone as far as the world is
                    // concerned, and whoever watched it fall still has it.
                    if (!knows && distance <= VicinityRadius && !Fallen(subject))
                    {
                        lock (this.introducedLock)
                        {
                            known.Add(subject.Identity);
                        }

                        this.Introduce(player.Controller.Client, subject);
                        continue;
                    }

                    if (knows && distance > ForgetRadius)
                    {
                        lock (this.introducedLock)
                        {
                            known.Remove(subject.Identity);
                        }

                        // Straight to the client, the way Introduce sends. Through the playfield bus
                        // this waited behind everything queued there, so a character walking out and
                        // back in could be introduced again before the older despawn went out, and
                        // then disappear for that client while the server believed it knew it.
                        player.Controller.Client.SendCompressed(DespawnMessageHandler.Default.Create(subject.Identity));
                    }
                }
            }
        }

        /// <summary>
        /// A character has been killed.
        /// </summary>
        /// <remarks>
        /// Only spawned characters come back. A player that dies is somebody
        /// else business - there is a whole resurrection sequence that belongs to
        /// it - and a player has no spawn point to come back to anyway.
        ///
        /// Timers stop. A dead character with a heal delta is a character that
        /// quietly regenerates back to life on the heartbeat, which is not a
        /// theory: the regeneration in HeartBeatTimer does not ask whether the
        /// thing it is healing is alive.
        /// </remarks>
        public void Died(ICharacter victim)
        {
            if (victim == null || !this.spawnPoint.ContainsKey(victim.Identity))
            {
                return;
            }

            victim.DoNotDoTimers = true;

            lock (this.deadLock)
            {
                this.bodyGoesAt[victim.Identity] = DateTime.UtcNow
                                                   + TimeSpan.FromSeconds(CorpseLingerSeconds);
                this.risesAt[victim.Identity] = DateTime.UtcNow + TimeSpan.FromSeconds(RespawnSeconds);
            }
        }

        /// <summary>
        /// Take away the bodies that have lain long enough, and put back the
        /// characters whose spawn points are due.
        /// </summary>
        /// <remarks>
        /// Nothing used to do either. A killed character stayed at zero health
        /// where it fell, for as long as the zone was up, and was still
        /// introduced to every arriving player as a member of the playfield.
        /// Arete Landing emptied out one Cleaning Robot at a time.
        ///
        /// Coming back is the same character rather than a new one. The live
        /// server issues a fresh identity each time, which is why a fifteen
        /// minute capture holds forty six Burning Cleaning Robots standing at
        /// three coordinates; here the identity of a spawned character is its
        /// spawn row, and reusing it is both simpler and kinder to a client that
        /// has just been told to forget the last one.
        /// </remarks>
        private void RaiseTheDead()
        {
            List<Identity> bodies = null;
            List<Identity> rising = null;

            lock (this.deadLock)
            {
                foreach (var due in this.bodyGoesAt)
                {
                    if (due.Value <= DateTime.UtcNow)
                    {
                        bodies = bodies ?? new List<Identity>();
                        bodies.Add(due.Key);
                    }
                }

                foreach (var due in this.risesAt)
                {
                    if (due.Value <= DateTime.UtcNow)
                    {
                        rising = rising ?? new List<Identity>();
                        rising.Add(due.Key);
                    }
                }

                if (bodies != null)
                {
                    foreach (Identity gone in bodies)
                    {
                        this.bodyGoesAt.Remove(gone);
                    }
                }

                if (rising != null)
                {
                    foreach (Identity back in rising)
                    {
                        this.risesAt.Remove(back);
                    }
                }
            }

            if (bodies != null)
            {
                foreach (Identity gone in bodies)
                {
                    var corpse = new Identity
                                 {
                                     Type = IdentityType.Corpse,
                                     Instance = gone.Instance
                                 };

                    // The body and its transient inventory are separate pooled
                    // identities. Expire both at the same point; otherwise the
                    // invisible inventory remains addressable until this spawn
                    // dies again.
                    this.Announce(DespawnMessageHandler.Default.Create(corpse));
                    CorpseLifecycle.Expire(this.Identity, corpse);
                    this.Despawn(gone);
                }
            }

            if (rising == null)
            {
                return;
            }

            foreach (Identity back in rising)
            {
                var character = this.FindByIdentity<ICharacter>(back);
                if (character == null)
                {
                    continue;
                }

                // Whole again, and back where the spawn point put it. The
                // streaming pass introduces it to whoever is near enough on its
                // next turn - there is nothing to send from here.
                character.Stats[StatIds.health].Value = this.SpawnHealth(character);
                character.Stats[StatIds.currentnano].Value = character.Stats[StatIds.maxnanoenergy].Value;

                Coordinate where;
                if (this.spawnPoint.TryGetValue(back, out where))
                {
                    character.Coordinates(where);
                }

                OmniCell.Core.Vector.Quaternion facing;
                if (this.spawnHeading.TryGetValue(back, out facing))
                {
                    character.RawHeading = facing;
                }

                character.DoNotDoTimers = false;
            }
        }

        /// <summary>
        /// Drop a character out of everything this playfield remembers about who
        /// can see whom.
        /// </summary>
        /// <remarks>
        /// Both directions. A character that has left is no longer anybody else
        /// business, and a player that has left has no beliefs left to keep
        /// track of - and if its set were left behind, the next character to
        /// take that identity would inherit it.
        /// </remarks>
        private void Forget(Identity gone)
        {
            lock (this.introducedLock)
            {
                this.introduced.Remove(gone);
                foreach (HashSet<Identity> known in this.introduced.Values)
                {
                    known.Remove(gone);
                }
            }
        }

        /// <summary>
        /// Whether a character should not be told this yet.
        /// </summary>
        /// <remarks>
        /// A character being walked into the world hears about itself and
        /// nothing else. Until ClientConnected has finished, the client has not
        /// been introduced to anybody in the playfield, and a message about a
        /// stranger is at best noise and at worst a stat update for a character
        /// the client has never heard of.
        ///
        /// It is not a small amount of noise. Four hundred and thirty eight
        /// characters regenerating on a ten millisecond heartbeat put their stat
        /// messages through the entry sequence the whole way: the stream the
        /// client was reading had them interleaved from before PlayfieldAnarchyF
        /// onwards, in among the vendors and the doors and the world. The
        /// captures have none - the entry sequence there is uninterrupted from
        /// ChatServerInfo to CharInPlay.
        ///
        /// The character's own messages still go through, because the entry
        /// sequence itself announces some of them: its own full update, its own
        /// appearance.
        /// </remarks>
        private static bool Deaf(ICharacter listener, MessageBody messageBody)
        {
            if (!listener.EnteringWorld)
            {
                return false;
            }

            var n3 = messageBody as N3Message;
            return n3 == null || n3.Identity != listener.Identity;
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        public void AnnounceAppearanceUpdate(ICharacter character)
        {
            AppearanceUpdateMessageHandler.Default.Send(character);
        }

        /// <summary>
        /// </summary>
        /// <param name="messageBody">
        /// </param>
        /// <param name="dontSend">
        /// </param>
        public void AnnounceOthers(MessageBody messageBody, Identity dontSend)
        {
            Coordinate origin;
            Identity subject;
            bool located = this.Subject(messageBody, out subject, out origin);

            foreach (Character entity in
                Pool.Instance.GetAll<Character>((int)IdentityType.CanbeAffected)
                    .Where(xx => xx.InPlayfield(this.Identity)))
            {
                if (entity != null)
                {
                    // The null check is not decoration: a spawned character has
                    // a controller and no client, and there are several hundred
                    // of them in a playfield.
                    if (entity.Identity != dontSend && entity.Controller.Client != null
                        && !Deaf(entity, messageBody)
                        && (!located || this.Knows(entity.Identity, subject)))
                    {
                        this.Publish(
                            new IMSendAOtomationMessageBodyToClient()
                            {
                                client = entity.Controller.Client,
                                Body = messageBody
                            });
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="identity">
        /// </param>
        public void Despawn(Identity identity)
        {
            // Anything the server was still doing on this character's behalf ends
            // here. A fight or a cast left behind keeps being ticked for a
            // character that is no longer in the playfield.
            Combat.Forget(identity);
            NanoCasting.Forget(identity);
            SurgeryClinic.Forget(identity);
            WoundedCharacters.Forget(identity);
            QuestManager.Forget(identity);
            Pets.Forget(identity);
            CorpseLootAccess.ForgetCharacter(identity);

            // Everyone who was told about this one is told to forget it. Sent
            // before the set is cleared, because after it is cleared Announce
            // has nobody to send it to.
            this.Announce(DespawnMessageHandler.Default.Create(identity));
            this.Forget(identity);
        }

        /// <summary>
        /// </summary>
        public void DisconnectAllClients()
        {
            IEnumerable<Character> templist = Pool.Instance.GetAll<Character>((int)IdentityType.CanbeAffected).ToList();
            for (int i = templist.Count() - 1; i >= 0; i--)
            {
                IEntity entity = templist.ElementAt(i);
                if ((entity as Character) != null)
                {
                    if ((entity as Character).Controller.Client != null)
                    {
                        this.server.DisconnectClient((entity as Character).Controller.Client);
                    }
                    (entity as Character).Dispose();
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="identity">
        /// </param>
        /// <returns>
        /// </returns>
        public IInstancedEntity FindByIdentity(Identity identity)
        {
            return Pool.Instance.GetObject<IInstancedEntity>(identity);
        }

        /// <summary>
        /// </summary>
        /// <param name="identity">
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public T FindByIdentity<T>(Identity identity) where T : class, IEntity
        {
            return Pool.Instance.GetObject<T>(identity);
        }

        /// <summary>
        /// </summary>
        /// <param name="dynel">
        /// </param>
        /// <param name="range">
        /// </param>
        /// <returns>
        /// </returns>
        public List<IDynel> FindInRange(IDynel dynel, float range)
        {
            List<IDynel> temp = new List<IDynel>();
            Coordinate coord = dynel.Coordinates();
            foreach (Dynel entity in
                Pool.Instance.GetAll<Dynel>((int)IdentityType.CanbeAffected).Where(xx => xx.InPlayfield(this.Identity)))
            {
                if (entity == dynel)
                {
                    continue;
                }

                if (entity.Coordinates().Distance2D(coord) <= range)
                {
                    temp.Add(entity);
                }
            }

            return temp;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public bool IsInstancedPlayfield()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public int NumberOfDynels()
        {
            return Pool.Instance.GetAll((int)IdentityType.CanbeAffected).Count();
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public int NumberOfPlayers()
        {
            return Pool.Instance.GetAll<Character>((int)IdentityType.CanbeAffected).Count();
        }

        /// <summary>
        /// </summary>
        /// <param name="obj">
        /// </param>
        public void Publish(object obj)
        {
            // A message for the whole playfield becomes one message per listener here, as it is sent.
            // Queued as a single message and only expanded when the queue reached it, each copy went to
            // the back of the queue at that point - behind messages sent after it - and who could see it
            // was decided then rather than when it was sent.
            var toPlayfield = obj as IMSendAOtomationMessageToPlayfield;
            if (toPlayfield != null)
            {
                this.Announce(toPlayfield.Body);
                return;
            }

            var toOthers = obj as IMSendAOtomationMessageToPlayfieldOthers;
            if (toOthers != null)
            {
                this.AnnounceOthers(toOthers.Body, toOthers.Identity);
                return;
            }

            this.playfieldBus.Publish(obj);
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="body">
        /// </param>
        public void Send(IZoneClient client, MessageBody body)
        {
            this.Publish(new IMSendAOtomationMessageBodyToClient() { client = client, Body = body });
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="message">
        /// </param>
        public void Send(IZoneClient client, Message message)
        {
            this.Publish(new IMSendAOtomationMessageToClient() { client = client, message = message });
        }

        /// <summary>
        /// </summary>
        /// <param name="dynel">
        /// </param>
        /// <param name="destination">
        /// </param>
        /// <param name="heading">
        /// </param>
        /// <param name="playfield">
        /// </param>
        public void Teleport(Dynel dynel, Coordinate destination, IQuaternion heading, Identity playfield)
        {
            // Prevent client from entering this again
            if (dynel.DoNotDoTimers)
            {
                return;
            }

            // Disable sending stat changes at once, then give what is already queued for the client time
            // to go out before the teleport. The wait used to be two Thread.Sleeps, holding whatever called
            // this - the client's own message queue, or the playfield's heartbeat for a wall - for 1.2
            // seconds. The rest now runs after the delay, on the client's queue, and nothing is held.
            dynel.DoNotDoTimers = true;
            IZoneClient waitingClient = dynel.Controller == null ? null : dynel.Controller.Client;
            if (waitingClient != null)
            {
                waitingClient.Later(TeleportDelayMs, () => this.CompleteTeleport(dynel, destination, heading, playfield));
            }
            else
            {
                System.Threading.Tasks.Task.Delay(TeleportDelayMs).ContinueWith(
                    delay =>
                    {
                        try
                        {
                            this.CompleteTeleport(dynel, destination, heading, playfield);
                        }
                        catch (Exception e)
                        {
                            LogUtil.ErrorException(e, "Teleport of {0} failed", dynel.Identity.Instance);
                        }
                    });
            }
        }

        /// <summary>
        /// How long a teleport waits, so the messages queued for the client go out first.
        /// </summary>
        private const int TeleportDelayMs = 1200;

        /// <summary>
        /// The teleport itself, once Teleport's delay has passed.
        /// </summary>
        private void CompleteTeleport(Dynel dynel, Coordinate destination, IQuaternion heading, Identity playfield)
        {
            // Teleport to another playfield
            TeleportMessageHandler.Default.Send(
                dynel as ICharacter,
                destination.coordinate,
                (Vector.Quaternion)heading,
                playfield);

            // Send packet, disconnect, and other playfield waits for connect

            DespawnMessage despawnMessage = DespawnMessageHandler.Default.Create(dynel.Identity);
            this.AnnounceOthers(despawnMessage, dynel.Identity);
            dynel.RawCoordinates = new Vector3() { X = destination.x, Y = destination.y, Z = destination.z };
            dynel.RawHeading = new Vector.Quaternion(heading.xf, heading.yf, heading.zf, heading.wf);

            // IMPORTANT!!
            // Dispose the character object, save new playfield data and then recreate it
            // else you would end up at weird coordinates in the same playfield

            // Save client object
            ZoneClient client = (ZoneClient)dynel.Controller.Client;

            // Set client=null so dynel can really dispose

            IPlayfield newPlayfield = this.server.PlayfieldById(playfield);
            Pool.Instance.GetObject<Playfield>(
                Identity.None,
                new Identity() { Type = playfield.Type, Instance = playfield.Instance });

            if (newPlayfield == null)
            {
                newPlayfield = new Playfield(this.server, playfield);
            }

            dynel.Playfield = newPlayfield;
            dynel.Controller.Client = null;
            dynel.IsTeleporting = true;
            dynel.Dispose();

            LogUtil.Debug(DebugInfoDetail.Database, "Saving to pf " + playfield.Instance);

            // TODO: Get new server ip from chatengine (which has to log all zoneengine's playfields)
            // for now, just transmit our ip and port

            IPAddress tempIp;
            if (IPAddress.TryParse(Config.Instance.CurrentConfig.ZoneIP, out tempIp) == false)
            {
                IPHostEntry zoneHost = Dns.GetHostEntry(Config.Instance.CurrentConfig.ZoneIP);
                foreach (IPAddress ip in zoneHost.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        tempIp = ip;
                        break;
                    }
                }
            }

            var redirect = new ZoneRedirectionMessage
                           {
                               ServerIpAddress = tempIp,
                               ServerPort = (ushort)this.server.TcpEndPoint.Port
                           };
            if (client != null)
            {
                client.SendCompressed(redirect);
            }
            // client.Server.DisconnectClient(client);
        }

        /// <summary>
        /// </summary>
        /// <param name="clientMessage">
        /// </param>
        public static void SendAOtomationMessageToClient(IMSendAOtomationMessageToClient clientMessage)
        {
            LogUtil.Debug(DebugInfoDetail.AoTomation, clientMessage.message.Body.GetType().ToString());
            clientMessage.client.SendCompressed(clientMessage.message.Body);
        }

        /// <summary>
        /// </summary>
        /// <param name="entity">
        /// </param>
        public void DisconnectClient(IInstancedEntity entity)
        {
            Pool.Instance.RemoveObject(entity);
        }

        /// <summary>
        /// </summary>
        /// <param name="imExecuteFunction">
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public void ExecuteFunction(IMExecuteFunction imExecuteFunction)
        {
            var user = (ITargetingEntity)this.FindNamedEntityByIdentity(imExecuteFunction.User);
            INamedEntity target;

            // TODO: Go over the targets, they can return item templates, inventory entries etc too
            switch (imExecuteFunction.Function.Target)
            {
                case 1:
                    target = (INamedEntity)user;
                    break;
                case 2:
                    throw new NotImplementedException("Target Wearer not implemented yet");
                case 3:
                    target = this.FindNamedEntityByIdentity(user.SelectedTarget);
                    break;
                case 14:
                    target = this.FindNamedEntityByIdentity(user.FightingTarget);
                    break;
                case 19: // Perhaps (if issued from a item) its the item itself
                    target = (INamedEntity)user;
                    break;
                case 23:
                    target = this.FindNamedEntityByIdentity(user.SelectedTarget);
                    break;
                case 26:
                    target = (INamedEntity)user;
                    break;
                case 100:
                    target = (INamedEntity)user;
                    break;
                default:
                    throw new NotImplementedException(
                        "Unknown target encountered: Target#:" + imExecuteFunction.Function.Target);
            }

            if (target == null)
            {
                var temp = user as Character;
                if (temp != null)
                {
                    if (temp.Controller.Client != null)
                    {
                        temp.Controller.Client.SendCompressed(
                            new ChatTextMessage { Identity = temp.Identity, Text = "No valid target found" });
                    }
                    return;
                }
            }

            FunctionCollection.Instance.CallFunction(
                imExecuteFunction.Function.FunctionType,
                (INamedEntity)user,
                (INamedEntity)user,
                target,
                imExecuteFunction.Function.Arguments.Values.ToArray());
        }

        public List<ICharacter> FindCharacterInRange(IDynel dynel, float range)
        {
            List<ICharacter> temp = new List<ICharacter>();
            Coordinate coord = dynel.Coordinates();
            foreach (ICharacter entity in
                Pool.Instance.GetAll<ICharacter>((int)IdentityType.CanbeAffected)
                    .Where(xx => xx.InPlayfield(this.Identity)))
            {
                if (entity == dynel)
                {
                    continue;
                }

                if (((Character)entity).Coordinates().Distance2D(coord) <= range)
                {
                    temp.Add((Character)entity);
                }
            }

            return temp;
        }

        /// <summary>
        /// </summary>
        /// <param name="identity">
        /// </param>
        /// <returns>
        /// </returns>
        public INamedEntity FindNamedEntityByIdentity(Identity identity)
        {
            return Pool.Instance.GetObject<INamedEntity>(identity);
        }

        /// <summary>
        /// </summary>
        /// <param name="global">
        /// </param>
        /// <returns>
        /// </returns>
        public Dictionary<Identity, string> ListAvailablePlayfields(bool global = true)
        {
            return this.server.ListAvailablePlayfields(global);
        }

        /// <summary>
        /// </summary>
        /// <param name="msg">
        /// </param>
        public void SendAOtMessageBodyToClient(IMSendAOtomationMessageBodyToClient msg)
        {
            msg.client.SendCompressed(msg.Body);
        }

        /// <summary>
        /// </summary>
        /// <param name="msg">
        /// </param>
        public void SendAOtomationMessageBodiesToClient(IMSendAOtomationMessageBodiesToClient msg)
        {
            foreach (MessageBody mb in msg.Bodies)
            {
                msg.client.SendCompressed(mb);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="msg">
        /// </param>
        public void SendAOtomationMessageBodyToClient(IMSendAOtomationMessageBodyToClient msg)
        {
            if (msg.client != null)
            {
                try
                {
                    LogUtil.Debug(DebugInfoDetail.AoTomation, msg.Body.GetType().ToString());
                    msg.client.SendCompressed(msg.Body);
                }
                catch (Exception e)
                {
                    LogUtil.Debug(
                        DebugInfoDetail.Error,
                        msg.Body.GetType().ToString() + Environment.NewLine + e.Message);
                    // /!\ This happens sometimes, dont know why tho, need more investigation
                    // throw;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="clientMessage">
        /// </param>
        public void SendAOtomationMessageToPlayfield(IMSendAOtomationMessageToPlayfield clientMessage)
        {
            this.Announce(clientMessage.Body);
        }

        /// <summary>
        /// </summary>
        /// <param name="clientMessage">
        /// </param>
        public void SendAOtomationMessageToPlayfieldOthers(IMSendAOtomationMessageToPlayfieldOthers clientMessage)
        {
            this.AnnounceOthers(clientMessage.Body, clientMessage.Identity);
        }

        /// <summary>
        /// </summary>
        /// <param name="sendSCFUs">
        /// </param>
        public void SendSCFUsToClient(IMSendPlayerSCFUs sendSCFUs)
        {
            // Two things this used to get wrong, and they compounded.
            //
            // It walked the whole pool rather than this playfield, so a client
            // entering one playfield was introduced to the characters of every
            // other one as well. That is invisible while there is a single
            // playfield loaded and ruinous the moment there is a second.
            //
            // And it sent all of them. Arete Landing holds four hundred and
            // eighty three characters; the live server introduces between twenty
            // six and forty nine on entry and streams the rest in as the player
            // walks. See Stream, which does the streaming, and VicinityRadius,
            // which is where the number comes from.
            ICharacter player = sendSCFUs.toClient.Controller.Character;
            Identity dontSendTo = player.Identity;
            HashSet<Identity> known = this.Known(dontSendTo);
            var from = new Coordinate(player.RawCoordinates);

            foreach (IEntity entity in
                Pool.Instance.GetAll<IPacketReceivingEntity>(this.Identity, (int)IdentityType.CanbeAffected))
            {
                if (entity.Identity != dontSendTo)
                {
                    var temp = entity as Character;
                    if (temp != null
                        && from.Distance2D(new Coordinate(temp.RawCoordinates)) <= VicinityRadius)
                    {
                        lock (this.introducedLock)
                        {
                            known.Add(temp.Identity);
                        }

                        // TODO: make it NPC-safe
                        SimpleCharFullUpdateMessage simpleCharFullUpdate = SimpleCharFullUpdate.ConstructMessage(temp);
                        sendSCFUs.toClient.SendCompressed(simpleCharFullUpdate);

                        // Straight after the character, the thing in its hands.
                        // That is the order the live server uses, and without it
                        // every NPC in the playfield is drawn unarmed.
                        foreach (DBMobSpawnWeapon weapon in this.WeaponsOf(temp.Identity))
                        {
                            WeaponItemFullUpdateMessageHandler.Default.Send(
                                sendSCFUs.toClient.Controller.Character,
                                temp,
                                weapon);
                        }

                        // No CharInPlay here. It is not "here is a character" -
                        // the live server sends exactly one per session, for the
                        // player, at the very end of entry, and the client
                        // answers it to say it has finished loading. OmniCell
                        // sent one after every character in the playfield: 438 of
                        // them for Arete Landing, none of them the player's, in
                        // the middle of the world data. The client waited on the
                        // loading screen for the one that mattered and eventually
                        // gave up. ClientConnected sends that one now.
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="dynel">
        /// </param>
        private void CheckStatelCollision(ICharacter dynel)
        {
            foreach (StatelData sd in this.statels)
            {
                foreach (Event ev in
                    sd.Events.Where(
                        x =>
                            (x.EventType == EventType.OnCollide) || (x.EventType == EventType.OnEnter)
                            || (x.EventType == EventType.OnTargetInVicinity)))
                {
                    if (sd.Coord().Distance3D(dynel.Coordinates()) < 2.0f)
                    {
                        LogUtil.Debug(DebugInfoDetail.Statel, "Stepped on Statel " + sd.Identity.ToString(true));
                        LogUtil.Debug(DebugInfoDetail.Statel, ev.ToString());
                        ev.Perform(dynel, sd);
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="dynel">
        /// </param>
        private void CheckWallCollision(ICharacter dynel)
        {
            WallCollisionResult wcr = WallCollision.CheckCollision(
                dynel.Coordinates(),
                dynel.Playfield.Identity.Instance);
            if (wcr != null)
            {
                int destPlayfield = wcr.SecondWall.DestinationPlayfield;
                if (destPlayfield > 0)
                {
                    LogUtil.Debug(DebugInfoDetail.Zoning, wcr.ToString());

                    PlayfieldDestination dest =
                        PlayfieldLoader.PFData[destPlayfield].Destinations[wcr.SecondWall.DestinationIndex];

                    LogUtil.Debug(DebugInfoDetail.Zoning, dest.ToString());

                    float newX = (dest.EndX - dest.StartX) * wcr.Factor + dest.StartX;
                    float newZ = (dest.EndZ - dest.StartZ) * wcr.Factor + dest.StartZ;
                    float dist = WallCollision.Distance(dest.StartX, dest.StartZ, dest.EndX, dest.EndZ);
                    float headDistX = (dest.EndX - dest.StartX) / dist;
                    float headDistZ = (dest.EndZ - dest.StartZ) / dist;
                    newX -= headDistZ * 8;
                    newZ += headDistX * 8;

                    Coordinate destinationCoordinate = new Coordinate(newX, dynel.RawCoordinates.Y, newZ);

                    this.Teleport(
                        (Character)dynel,
                        destinationCoordinate,
                        dynel.RawHeading,
                        new Identity() { Type = IdentityType.Playfield, Instance = destPlayfield });
                    return;
                }
            }
        }

        /// <summary>
        /// The health a character is whole at: what its spawn point made it with, or its most.
        /// </summary>
        public int SpawnHealth(ICharacter character)
        {
            int health;
            lock (this.spawnHealth)
            {
                if (this.spawnHealth.TryGetValue(character.Identity, out health))
                {
                    return health;
                }
            }

            return character.Stats[StatIds.life].Value;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender">
        /// </param>
        private void HeartBeatTimer(object sender)
        {
            IEnumerable<IEntity> dynels = null;
            dynels =
                Pool.Instance.GetAll<ICharacter>((int)IdentityType.CanbeAffected)
                    .Where(xx => !xx.DoNotDoTimers && xx.InPlayfield(this.Identity));

            foreach (ICharacter dynel in dynels)
            {
                if (dynel == null)
                {
                    continue;
                }

                // One character the stat formulas cannot handle used to take the
                // whole zone down with it. This runs on a timer thread, so an
                // exception here is unhandled and the process ends - the first
                // Cleaning Robot to spawn in Arete Landing did exactly that,
                // twice, from two different tables.
                //
                // The formulas are guarded now, but the heartbeat should not be
                // the thing that finds the next one. A character that cannot
                // tick is worth a line in the log; it is not worth everyone
                // else's playfield.
                try
                {
                    if (dynel.DoNotDoTimers || dynel.Starting)
                    {
                        continue;
                    }

                    // Regeneration only counts as a change if it changed
                    // something. A character at full health ticks like any other,
                    // and telling the playfield about it every time meant four
                    // hundred and forty stat messages every few seconds, to
                    // everybody, saying nothing - which is a great deal of a
                    // client's attention to spend on a Cleaning Robot that is
                    // perfectly well.
                    bool changed = false;
                    StatHealInterval healInterval = (StatHealInterval)dynel.Stats[StatIds.healinterval];
                    if (healInterval.LastTick < DateTime.UtcNow)
                    {
                        int interval = healInterval.Value;
                        int delta = dynel.Stats[StatIds.healdelta].Value;
                        int before = dynel.Stats[StatIds.health].Value;
                        int ceiling = this.SpawnHealth(dynel);
                        if (before < ceiling)
                        {
                            dynel.Stats[StatIds.health].Value = Math.Min(before + delta, ceiling);
                        }
                        healInterval.LastTick = DateTime.UtcNow + TimeSpan.FromSeconds(interval);
                        changed |= dynel.Stats[StatIds.health].Value != before;
                    }

                    StatNanoInterval nanoInterval = (StatNanoInterval)dynel.Stats[StatIds.nanointerval];
                    if (nanoInterval.LastTick < DateTime.UtcNow)
                    {
                        int interval = nanoInterval.Value;
                        int delta = dynel.Stats[StatIds.nanodelta].Value;
                        int before = dynel.Stats[StatIds.currentnano].Value;
                        dynel.Stats[StatIds.currentnano].Value += delta;
                        nanoInterval.LastTick = DateTime.UtcNow + TimeSpan.FromSeconds(interval);
                        changed |= dynel.Stats[StatIds.currentnano].Value != before;
                    }

                    if (changed)
                    {
                        dynel.SendChangedStats();
                    }

                    if (dynel.Controller.IsFollowing())
                    {
                        dynel.Controller.DoFollow();
                    }
                    else
                    {
                        if (dynel.Controller is NPCController)
                        {
                            if (dynel.Controller.State == CharacterState.Patrolling)
                            {
                                dynel.Controller.StartPatrolling();
                            }
                        }
                    }

                    // Auto attack. The client sends AttackMessage once and
                    // then says nothing per swing, so every swing after the
                    // first is scheduled here.
                    Combat.Tick(dynel);

                    // Nano casts finish on the same clock, for the same reason:
                    // the client says nothing between starting a cast and the
                    // server telling it the nano landed.
                    NanoCasting.Tick(dynel);

                    // Experience is checked against the level table here for the
                    // same reason: nothing else notices when it changes.
                    Leveling.Tick(dynel);

                    if (dynel.Controller is PlayerController)
                    {
                        this.Keepalive(dynel);
                        this.CheckWallCollision(dynel);
                        this.CheckStatelCollision(dynel);
                    }
                }
                catch (Exception exception)
                {
                    LogUtil.ErrorException(exception);
                }
            }
            // The dead, then who can see whom - in that order, so a body taken
            // away this pass is not introduced to somebody by the next one.
            try
            {
                this.RaiseTheDead();
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
            }

            try
            {
                this.Stream();
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
            }

            try
            {
                this.heartBeat.Change(HeartBeatMilliseconds, 0);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    // We wont save any NPCs to character table/character's stats table
                    this.DisconnectAllClients();
                    if (this.memBusDisposeContainer != null)
                    {
                        this.memBusDisposeContainer.Dispose();
                    }
                    if (this.heartBeat != null)
                    {
                        this.heartBeat.Dispose();
                    }
                }
            }
            this.disposed = true;

            base.Dispose(disposing);
        }
    }
}
