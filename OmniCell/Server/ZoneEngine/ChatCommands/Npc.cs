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

namespace ZoneEngine.ChatCommands
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Data.Linq;
    using System.Linq;
    using System.Text;

    using OmniCell.Core.NPCHandler;
    using OmniCell.Enums;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Vector;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Script;

    #endregion

    public class Npc : AOChatCommand
    {
        public override bool CheckCommandArguments(string[] args)
        {
            // A name can be several words, so there is no upper bound. The
            // verb itself is all this can check for.
            return args.Length >= 2;
        }

        public override void CommandHelp(ICharacter character)
        {
            Tell(
                character,
                "/npc create <name>      a new character, standing where you are\n"
                + "/npc model              the models in the world, most used first\n"
                + "/npc model <text>       only the ones somebody called <text> uses\n"
                + "/npc model <id> [head]  give the targeted character that model\n"
                + "/npc name <name>        rename the targeted character\n"
                + "/npc save               write the targeted character down as it stands\n"
                + "/npc remove             take the targeted character's spawn point away\n"
                + "/npc knubot <script>    hand the targeted character a compiled script\n"
                + "\nGive it something to say with /questedit.");
        }

        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            string verb = args[1].ToLower();
            string tail = string.Join(" ", args.Skip(2));

            if (verb == "create")
            {
                this.Create(character, tail);
                return;
            }

            if (verb == "model")
            {
                this.Model(character, target, args, tail);
                return;
            }

            if (verb == "name")
            {
                this.Rename(character, target, tail);
                return;
            }

            if (args[1].ToLower() == "save")
            {
                Character mob = Pool.Instance.GetObject<Character>(character.Playfield.Identity, target);
                if (mob == null)
                {
                    character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, "Not a NPC?"));
                    return;
                }

                if (!(mob.Controller is NPCController))
                {
                    character.Playfield.Publish(
                        ChatTextMessageHandler.Default.CreateIM(
                            character,
                            "Don't try to remove/save other players please."));
                    return;
                }

                DBMobSpawn mobdbo = new DBMobSpawn();
                mobdbo.Id = mob.Identity.Instance;
                mobdbo.Name = mob.Name;
                mobdbo.Textures0 = 0;
                mobdbo.Textures1 = 0;
                mobdbo.Textures2 = 0;
                mobdbo.Textures3 = 0;
                mobdbo.Textures4 = 0;
                mobdbo.Playfield = mob.Playfield.Identity.Instance;
                Coordinate tempCoordinate = mob.Coordinates();
                mobdbo.X = tempCoordinate.x;
                mobdbo.Y = tempCoordinate.y;
                mobdbo.Z = tempCoordinate.z;
                mobdbo.HeadingW = mob.Heading.wf;
                mobdbo.HeadingX = mob.Heading.xf;
                mobdbo.HeadingY = mob.Heading.yf;
                mobdbo.HeadingZ = mob.Heading.zf;
                if (mob.Waypoints.Count > 0)
                {
                    List<MobSpawnWaypoint> temp = this.GetMobWaypoints(mob);
                    mobdbo.Waypoints = new Binary(MessagePackZip.SerializeData(temp));
                }

                if (MobSpawnDao.Instance.Exists(mobdbo.Id))
                {
                    MobSpawnDao.Instance.Delete(mobdbo.Id);
                }

                MobSpawnDao.Instance.Add(mobdbo);

                // Clear remnants first
                MobSpawnStatDao.Instance.Delete(new { mobdbo.Id, mobdbo.Playfield });
                Dictionary<int, uint> statsToSave = mob.Stats.GetStatValues();
                foreach (KeyValuePair<int, uint> kv in statsToSave)
                {
                    MobSpawnStatDao.Instance.Add(
                        new DBMobSpawnStat()
                        {
                            Id = mob.Identity.Instance,
                            Playfield = mob.Playfield.Identity.Instance,
                            Stat = kv.Key,
                            Value = (int)kv.Value
                        });
                }
            }
            if (args[1].ToLower() == "remove")
            {
                MobSpawnDao.Instance.Delete(target.Instance);
            }

            if (args[1].ToLower() == "knubot")
            {
                ICharacter cmob = Pool.Instance.GetObject<ICharacter>(character.Playfield.Identity, target);
                if (cmob == null)
                {
                    character.Playfield.Publish(
                        ChatTextMessageHandler.Default.CreateIM(
                            character,
                            string.Format("Target {0} is no npc.", target.ToString(true))));
                    return;
                }

                string scriptname = args[2];
                scriptname = ScriptCompiler.Instance.ClassExists(scriptname);
                if (scriptname != "")
                {
                    DBMobSpawn mob = MobSpawnDao.Instance.Get(target.Instance);
                    if (mob == null)
                    {
                        character.Playfield.Publish(
                            ChatTextMessageHandler.Default.CreateIM(
                                character,
                                string.Format(
                                    "Target npc {0} is not yet saved to mobspawn table.",
                                    target.ToString(true))));
                    }
                    else
                    {
                        mob.KnuBotScriptName = scriptname;
                        MobSpawnDao.Instance.Save(mob);
                        character.Playfield.Publish(
                            ChatTextMessageHandler.Default.CreateIM(
                                character,
                                string.Format(
                                    "Saved initialization script '{0}' for spawn {1}.",
                                    args[2],
                                    target.ToString(true))));
                        ((NPCController)cmob.Controller).SetKnuBot(
                            ScriptCompiler.Instance.CreateKnuBot(scriptname, cmob.Identity));
                    }
                }
                else
                {
                    character.Playfield.Publish(
                        ChatTextMessageHandler.Default.CreateIM(
                            character,
                            string.Format("Script '{0}' does not exist.", args[2])));
                }
            }
        }

        /// <summary>
        /// Puts a new character in the world where the caller is standing, and
        /// writes down its spawn point.
        /// </summary>
        /// <remarks>
        /// Through mobspawns and NonPlayerCharacterHandler.InstantiateMobSpawn,
        /// which is what the playfield does with every row in that table when
        /// it loads. So a character made here is the same kind of thing as Rex
        /// Larsson, comes back after a restart without anything else being
        /// done, and can be handed a conversation by /questedit.
        /// </remarks>
        private void Create(ICharacter character, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Tell(character, "It needs a name: /npc create Sergeant Blank");
                return;
            }

            int playfield = character.Playfield.Identity.Instance;
            int id = NextSpawnId();

            Coordinate here = character.Coordinates();
            var spawn = new DBMobSpawn
                            {
                                Id = id,
                                Name = name,
                                Playfield = playfield,
                                X = here.x,
                                Y = here.y,
                                Z = here.z,
                                HeadingX = character.Heading.xf,
                                HeadingY = character.Heading.yf,
                                HeadingZ = character.Heading.zf,
                                HeadingW = character.Heading.wf,
                                KnuBotScriptName = string.Empty
                            };

            MobSpawnDao.Instance.Add(spawn);

            MobSpawnStatDao.Instance.Delete(new { Id = id, Playfield = playfield });
            foreach (KeyValuePair<int, int> stat in Plain)
            {
                MobSpawnStatDao.Instance.Add(
                    new DBMobSpawnStat { Id = id, Playfield = playfield, Stat = stat.Key, Value = stat.Value });
            }

            ICharacter made = NonPlayerCharacterHandler.InstantiateMobSpawn(
                spawn,
                MobSpawnStatDao.Instance.GetWhere(new { Id = id, Playfield = playfield }).ToArray(),
                new NPCController(),
                character.Playfield);

            if (made == null)
            {
                Tell(character, "Written down as " + id + ", but it would not spawn.");
                return;
            }

            Tell(
                character,
                "Created " + name + " (" + id + "). Target it and use /npc model to change how it looks, "
                + "or /questedit new <quest name> to give it something to say.");
        }

        /// <summary>
        /// Lists the models in use, or gives one to the targeted character.
        /// </summary>
        /// <remarks>
        /// There is no catalogue of models to pick from - the client reads them
        /// out of Funcom's own data and the server never sees a list. What the
        /// server does have is every model that anything in the world is
        /// wearing, which is the same thing from the other end: pick the
        /// character you want yours to look like and take the number off it.
        /// </remarks>
        private void Model(ICharacter character, Identity target, string[] args, string tail)
        {
            int model;
            if (!int.TryParse(args.Length > 2 ? args[2] : string.Empty, out model))
            {
                this.ListModels(character, tail);
                return;
            }

            ICharacter npc = Pool.Instance.GetObject<ICharacter>(character.Playfield.Identity, target);
            if (npc == null || !(npc.Controller is NPCController))
            {
                Tell(character, "Target the character you want to change first.");
                return;
            }

            int head = 0;
            bool hasHead = args.Length > 3 && int.TryParse(args[3], out head);

            Set(npc, (int)StatIds.monsterdata, model);
            if (hasHead)
            {
                Set(npc, (int)StatIds.headmesh, head);
            }

            // Told to forget it and shown it again. A model is part of the
            // character the client built when it was first introduced, and a
            // stat arriving afterwards does not rebuild it - the streaming pass
            // does, on its next round, because Despawn is also what makes it
            // forget who it has been introduced to.
            character.Playfield.Despawn(npc.Identity);

            Tell(
                character,
                npc.Name + " is now model " + model + (hasHead ? " with head " + head : string.Empty)
                + ". Give it a moment to come back.");
        }

        private void ListModels(ICharacter character, string filter)
        {
            var counts = new Dictionary<int, int>();
            var examples = new Dictionary<int, string>();

            // Both tables once each and joined here. Asking for one spawn's
            // model at a time is sixteen hundred queries to answer one line
            // typed into a chat window.
            var models = new Dictionary<string, int>();
            foreach (DBMobSpawnStat stat in
                MobSpawnStatDao.Instance.GetWhere(new { Stat = (int)StatIds.monsterdata }))
            {
                models[stat.Id + ":" + stat.Playfield] = stat.Value;
            }

            foreach (DBMobSpawn spawn in MobSpawnDao.Instance.GetAll())
            {
                if (filter.Length > 0 && spawn.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                int model;
                if (!models.TryGetValue(spawn.Id + ":" + spawn.Playfield, out model))
                {
                    continue;
                }

                counts[model] = counts.ContainsKey(model) ? counts[model] + 1 : 1;
                examples[model] = spawn.Name;
            }

            if (counts.Count == 0)
            {
                Tell(character, "Nothing in the world matches \"" + filter + "\".");
                return;
            }

            var text = new StringBuilder(
                filter.Length > 0
                    ? "Models worn by characters called \"" + filter + "\":\n"
                    : "Models in use, most used first:\n");

            foreach (var kv in counts.OrderByDescending(k => k.Value).Take(40))
            {
                text.AppendLine(
                    kv.Key + "   " + kv.Value + " of them, like " + examples[kv.Key]);
            }

            text.AppendLine();
            text.AppendLine("Target a character and /npc model <id> to give it one.");
            Tell(character, text.ToString());
        }

        private void Rename(ICharacter character, Identity target, string name)
        {
            ICharacter npc = Pool.Instance.GetObject<ICharacter>(character.Playfield.Identity, target);
            if (npc == null || !(npc.Controller is NPCController) || name.Length == 0)
            {
                Tell(character, "Target a character, then /npc name <what to call it>.");
                return;
            }

            DBMobSpawn spawn = MobSpawnDao.Instance.Get(npc.Identity.Instance);
            if (spawn == null)
            {
                Tell(character, "That one has no spawn point. /npc save it first.");
                return;
            }

            spawn.Name = name;
            MobSpawnDao.Instance.Save(spawn);
            npc.Name = name;
            character.Playfield.Despawn(npc.Identity);

            Tell(character, "Renamed to " + name + ".");
        }

        /// <summary>
        /// Changes a stat on a spawned character and on its spawn point, so it
        /// survives a restart.
        /// </summary>
        private static void Set(ICharacter npc, int stat, int value)
        {
            npc.Stats[stat].Value = value;

            int id = npc.Identity.Instance;
            int playfield = npc.Playfield.Identity.Instance;

            MobSpawnStatDao.Instance.Delete(new { Id = id, Playfield = playfield, Stat = stat });
            MobSpawnStatDao.Instance.Add(
                new DBMobSpawnStat { Id = id, Playfield = playfield, Stat = stat, Value = value });
        }

        /// <summary>
        /// The next spawn id nothing is using.
        /// </summary>
        /// <remarks>
        /// Above everything already written down, which puts characters made
        /// here clear of Funcom's own ids - Arete Landing's run from 2052536065
        /// up - without needing a range set aside for them.
        /// </remarks>
        private static int NextSpawnId()
        {
            int highest = MobSpawnDao.Instance.GetAll().Select(m => m.Id).DefaultIfEmpty(0).Max();
            return Math.Max(highest, 2100000000) + 1;
        }

        /// <summary>
        /// What a character looks like before anybody chooses.
        /// </summary>
        /// <remarks>
        /// Rex Larsson's own stats, less the ones that are about being Rex
        /// Larsson. Made up numbers would give a character that is invisible,
        /// or unkillable, or standing still at a hundred times its own size;
        /// these are a set the live server actually sent for somebody who
        /// stands in a room and talks to people, which is what a character made
        /// this way is for.
        /// </remarks>
        private static readonly Dictionary<int, int> Plain = new Dictionary<int, int>
                                                                 {
                                                                     { (int)StatIds.flags, 277615105 },
                                                                     { (int)StatIds.life, 511 },
                                                                     { (int)StatIds.breed, 1 },
                                                                     { (int)StatIds.health, 511 },
                                                                     { (int)StatIds.side, 0 },
                                                                     { (int)StatIds.fatness, 1 },
                                                                     { (int)StatIds.level, 15 },
                                                                     { (int)StatIds.sex, 2 },
                                                                     { (int)StatIds.headmesh, 40691 },
                                                                     { (int)StatIds.race, 1 },
                                                                     { (int)StatIds.runspeed, 52 },
                                                                     { (int)StatIds.maxdamage, 58 },
                                                                     { (int)StatIds.mindamage, 27 },
                                                                     { (int)StatIds.monsterdata, 26074 },
                                                                     { (int)StatIds.monsterscale, 97 },
                                                                     { (int)StatIds.npcfamily, 137 },
                                                                     { (int)StatIds.visualflags, 31 }
                                                                 };

        private static void Tell(ICharacter character, string text)
        {
            character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
        }

        private List<MobSpawnWaypoint> GetMobWaypoints(Character mob)
        {
            List<MobSpawnWaypoint> temp = new List<MobSpawnWaypoint>();
            foreach (Waypoint wp in mob.Waypoints)
            {
                MobSpawnWaypoint waypoint = new MobSpawnWaypoint();
                waypoint.Identity = mob.Identity.Instance;
                waypoint.Playfield = mob.Playfield.Identity.Instance;
                waypoint.WalkMode = wp.Running ? 1 : 0;
                waypoint.X = wp.Position.xf;
                waypoint.Y = wp.Position.yf;
                waypoint.Z = wp.Position.zf;
                temp.Add(waypoint);
            }
            return temp;
        }

        public override int GMLevelNeeded()
        {
            return 1;
        }

        public override List<string> ListCommands()
        {
            return new List<string> { "npc" };
        }
    }
}
