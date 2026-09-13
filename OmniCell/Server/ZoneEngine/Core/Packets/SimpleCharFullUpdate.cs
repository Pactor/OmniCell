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

namespace ZoneEngine.Core.Packets
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Nanos;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Network;
    using OmniCell.Core.Textures;
    using OmniCell.Core.Vector;
    using OmniCell.Enums;
    using OmniCell.Interfaces;
    using OmniCell.Stats;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using OmniCell.Database.Dao;

    using ZoneEngine.Core.Playfields;

    using Quaternion = OmniCell.Core.Vector.Quaternion;
    using Vector3 = SmokeLounge.AOtomation.Messaging.GameData.Vector3;

    #endregion

    /// <summary>
    /// </summary>
    public static class SimpleCharFullUpdate
    {
        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <returns>
        /// </returns>
        public static SimpleCharFullUpdateMessage ConstructMessage(Character character)
        {
            // No need to set packet flags here, its all done in the SimpleCharFullUpdateSerializer.cs
            // - Algorithman

            // Character Variables
            bool socialonly;
            bool showsocial;

            int charPlayfield;
            Coordinate charCoord;
            Identity charId;
            Quaternion charHeading;

            uint sideValue;
            uint fatValue;
            uint breedValue;
            uint sexValue;
            uint raceValue;

            string charName;
            int charFlagsValue;
            int accFlagsValue;

            int expansionValue;
            int currentNano;
            int currentHealth;

            uint strengthBaseValue;
            uint staminaBaseValue;
            uint agilityBaseValue;
            uint senseBaseValue;
            uint intelligenceBaseValue;
            uint psychicBaseValue;

            string firstName;
            string lastName;
            int orgNameLength;
            string orgName;
            int levelValue;
            int healthValue;
            int losHeight;

            int monsterData;
            int monsterScale;
            int visualFlags;

            int currentMovementMode;
            uint runSpeedBaseValue;

            int texturesCount;

            int headMeshValue;

            // NPC Values
            int NPCFamily;

            var socialTab = new Dictionary<int, int>();

            var textures = new List<AOTextures>();

            List<AOMeshs> meshs;

            var nanos = new List<AONano>();

            lock (character)
            {
                socialonly = (character.Stats[StatIds.visualflags].Value & 0x40) > 0;
                showsocial = (character.Stats[StatIds.visualflags].Value & 0x20) > 0;

                charPlayfield = character.Playfield.Identity.Instance;
                charCoord = character.Coordinates();
                charId = character.Identity;
                charHeading = character.Heading;

                sideValue = character.Stats[StatIds.side].BaseValue;
                fatValue = character.Stats[StatIds.fatness].BaseValue;
                breedValue = character.Stats[StatIds.breed].BaseValue;
                sexValue = character.Stats[StatIds.sex].BaseValue;
                raceValue = character.Stats[StatIds.race].BaseValue;

                charName = character.Name;
                charFlagsValue = character.Stats[StatIds.flags].Value;
                accFlagsValue = character.Stats[StatIds.accountflags].Value;

                expansionValue = character.Stats[StatIds.expansion].Value;
                currentNano = character.Stats[StatIds.currentnano].Value;

                strengthBaseValue = character.Stats[StatIds.strength].BaseValue;
                staminaBaseValue = character.Stats[StatIds.stamina].BaseValue;
                agilityBaseValue = character.Stats[StatIds.agility].BaseValue;
                senseBaseValue = character.Stats[StatIds.sense].BaseValue;
                intelligenceBaseValue = character.Stats[StatIds.intelligence].BaseValue;
                psychicBaseValue = character.Stats[StatIds.psychic].BaseValue;

                firstName = character.FirstName;
                lastName = character.LastName;
                orgNameLength = character.OrganizationName.Length;
                orgName = character.OrganizationName;
                levelValue = character.Stats[StatIds.level].Value;
                healthValue = character.Stats[StatIds.life].Value;

                monsterData = character.Stats[StatIds.monsterdata].Value;
                monsterScale = character.Stats[StatIds.monsterscale].Value;
                visualFlags = character.Stats[StatIds.visualflags].Value;

                currentMovementMode = character.Stats[StatIds.currentmovementmode].Value;
                runSpeedBaseValue = character.Stats[StatIds.runspeed].BaseValue;

                texturesCount = character.Textures.Count;

                headMeshValue = character.Stats[StatIds.headmesh].Value;

                foreach (int num in character.SocialTab.Keys)
                {
                    socialTab.Add(num, character.SocialTab[num]);
                }

                foreach (AOTextures at in character.Textures)
                {
                    textures.Add(new AOTextures(at.place, at.Texture));
                }

                meshs = MeshLayers.GetMeshs(character, showsocial, socialonly);

                foreach (KeyValuePair<int, IActiveNano> kv in character.ActiveNanos)
                {
                    var tempNano = new AONano();
                    tempNano.ID = kv.Value.ID;
                    tempNano.Instance = kv.Value.Instance;
                    tempNano.NanoStrain = kv.Key;
                    tempNano.Nanotype = kv.Value.Nanotype;
                    tempNano.TickCounter = kv.Value.TickCounter;
                    tempNano.TickInterval = kv.Value.TickInterval;
                    tempNano.Value3 = kv.Value.Value3;

                    nanos.Add(tempNano);
                }

                losHeight = character.Stats[StatIds.losheight].Value;
                NPCFamily = character.Stats[StatIds.npcfamily].Value;
                currentHealth = character.Stats[StatIds.health].Value;
            }

            var scfu = new SimpleCharFullUpdateMessage();

            // affected identity
            scfu.Identity = charId;

            // The client build this targets expects 58. Every SimpleCharFullUpdate
            // in a capture of 18.8.62 carries 58 and none carries 57 - 2803 of
            // them across four sessions, checked rather than assumed. 57 is what
            // CellAO sent to the client of its day.
            scfu.Version = 58;
            // The instance, not the playfield number, where the two differ.
            // Every character in the Arete Landing captures carries 2150461
            // here, which is the instance PlayfieldAnarchyF named, not 6553.
            // OmniCell sent 6553, so the characters it announced were in a
            // playfield the client had not been told it was standing in.
            scfu.PlayfieldId = Playfields.GetClientInstance(charPlayfield);

            if (character.FightingTarget.Instance != 0)
            {
                scfu.FightingTarget = new Identity
                                      {
                                          Type = character.FightingTarget.Type,
                                          Instance = character.FightingTarget.Instance
                                      };
            }

            // Coordinates
            scfu.Coordinates = new Vector3 { X = charCoord.x, Y = charCoord.y, Z = charCoord.z };

            // Heading Data
            scfu.Heading = new SmokeLounge.AOtomation.Messaging.GameData.Quaternion
                           {
                               W = charHeading.wf,
                               X = charHeading.xf,
                               Y = charHeading.yf,
                               Z = charHeading.zf
                           };

            // Race
            scfu.Appearance = new Appearance
                              {
                                  Side = (Side)sideValue,
                                  Fatness = (Fatness)fatValue,
                                  Breed = (Breed)breedValue,
                                  Gender = (Gender)sexValue,
                                  Race = raceValue
                              }; // appearance

            // Name
            scfu.Name = charName;

            scfu.CharacterFlags = (CharacterFlags)charFlagsValue; // Flags
            // 1234567890 is what the stat list uses for "nobody has set this",
            // and both of these fields are sixteen bits wide, so the sentinel
            // arrived at the client as its bottom half: 722, on every character
            // in the playfield, where the captures have zero. Say zero.
            scfu.AccountFlags = (short)StatValue.OrZero(accFlagsValue);
            scfu.Expansions = (short)expansionValue;

            bool isNpc = (NPCFamily != 1234567890) && (NPCFamily != 0);

            if (isNpc)
            {
                var snpc = new SimpleNpcInfo { Family = (short)NPCFamily, LosHeight = (short)StatValue.OrZero(losHeight) };
                scfu.CharacterInfo = snpc;
            }
            else
            {
                // Are we a player?
                var spc = new SimplePcInfo();

                spc.CurrentNano = (uint)currentNano; // CurrentNano
                spc.Team = 0; // team?
                spc.Swim = 5; // swim?

                // The checks here are to prevent the client doing weird things if the character has really large or small base attributes
                spc.StrengthBase = (short)Math.Min(strengthBaseValue, short.MaxValue); // Strength
                spc.AgilityBase = (short)Math.Min(agilityBaseValue, short.MaxValue); // Agility
                spc.StaminaBase = (short)Math.Min(staminaBaseValue, short.MaxValue); // Stamina
                spc.IntelligenceBase = (short)Math.Min(intelligenceBaseValue, short.MaxValue); // Intelligence
                spc.SenseBase = (short)Math.Min(senseBaseValue, short.MaxValue); // Sense
                spc.PsychicBase = (short)Math.Min(psychicBaseValue, short.MaxValue); // Psychic

                if (scfu.CharacterFlags.HasFlag(CharacterFlags.HasVisibleName))
                {
                    // has visible names? (Flags)
                    spc.FirstName = firstName;
                    spc.LastName = lastName;
                }

                if (orgNameLength != 0)
                {
                    spc.OrgName = orgName;
                }

                scfu.CharacterInfo = spc;
            }

            // Level
            scfu.Level = (short)levelValue;

            // Health
            scfu.Health = healthValue;

            scfu.HealthDamage = healthValue - currentHealth;

            // If player is in grid or fixer grid
            // make him/her/it a nice upside down pyramid
            if ((charPlayfield == 152) || (charPlayfield == 4107))
            {
                scfu.MonsterData = 99902;
            }
            else
            {
                scfu.MonsterData = (uint)monsterData; // Monsterdata
            }

            scfu.MonsterScale = (short)monsterScale; // Monsterscale
            scfu.VisualFlags = (short)visualFlags; // VisualFlags
            scfu.VisibleTitle = 0; // visible title?

            // The vehicle state: a Vector3, ten bytes of movement state - the
            // first of them the movement mode - an int32, and then four floats
            // for a player vehicle. 42 bytes. See
            // SimpleCharFullUpdateMessage.VehicleData for where each of those
            // is read.
            scfu.VehicleData = new byte[]
                            {
                                0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
                                (byte)currentMovementMode, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00,
                                0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                0x00, 0x00, 0x00, 0x00
                            };

            // An NPC vehicle ends in an int16 count instead of the four
            // floats, and sends it as zero: 28 bytes.
            if ((NPCFamily != 0) && (NPCFamily != 1234567890))
            {
                scfu.VehicleData = new byte[]
                                {
                                    // Knubot values??            
                                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                    (byte)currentMovementMode, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00,
                                    0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00
                                };
            }

            if (headMeshValue != 0)
            {
                scfu.HeadMesh = (uint?)headMeshValue; // Headmesh
            }

            // Runspeed
            scfu.RunSpeedBase = (short)runSpeedBaseValue;

            scfu.ActiveNanos = (from nano in nanos
                select
                    new SmokeLounge.AOtomation.Messaging.GameData.ActiveNano
                    {
                        NanoId = nano.ID,
                        NanoInstance = nano.Instance,
                        Time1 = nano.TickCounter,
                        Time2 = nano.TickInterval
                    }).ToArray();

            // Texture/Cloth Data
            var scfuTextures = new List<Texture>();

            var aotemp = new AOTextures(0, 0);
            for (int c = 0; c < 5; c++)
            {
                aotemp.Texture = 0;
                aotemp.place = c;
                for (int c2 = 0; c2 < texturesCount; c2++)
                {
                    if (textures[c2].place != c)
                    {
                        continue;
                    }

                    aotemp.Texture = textures[c2].Texture;
                    break;
                }

                if (showsocial)
                {
                    if (socialonly)
                    {
                        aotemp.Texture = socialTab[c];
                    }
                    else
                    {
                        if (socialTab[c] != 0)
                        {
                            aotemp.Texture = socialTab[c];
                        }
                    }
                }

                scfuTextures.Add(new Texture { Place = aotemp.place, Id = aotemp.Texture, Group = 0 });
            }

            // Five entries, even when every id in them is zero.
            //
            // A Cleaning Robot in the captures does carry an empty texture list,
            // and dropping ours to match looked right - but Bodyguard Logan Fixx,
            // in the same capture, carries five with every id zero, exactly as
            // this sends. The empty list goes with the HasExtendedTextures flag,
            // which the robot has and the bodyguard has not; it is not a rule
            // about the values. Trimming on the values instead was wrong and
            // cost a working login.
            scfu.Textures = scfuTextures.ToArray();

            // End Textures

            // ############
            // # Meshs
            // ############
            // A spawned character is drawn from a list that belongs to the
            // spawn, not from what it is wearing, because it is wearing nothing:
            // MeshLayers builds its answer out of equipment, and a mob has an
            // empty inventory, so every NPC in Arete Landing went out with a
            // single mesh whose id was zero. The captures carry the real lists
            // and they are in the playfield.
            var storedMeshes = character.Playfield is Playfield
                                   ? ((Playfield)character.Playfield).MeshesOf(character.Identity).ToList()
                                   : new List<DBMobSpawnMesh>();

            Mesh[] built = storedMeshes.Count > 0
                               ? (from stored in storedMeshes
                                  select
                                      new Mesh
                                      {
                                          Position = (byte)stored.Position,
                                          Id = (uint)stored.MeshId,
                                          OverrideTextureId = stored.OverrideTextureId,
                                          Layer = (byte)stored.Layer
                                      }).ToArray()
                               : (from aoMesh in meshs
                                  select
                                      new Mesh
                                      {
                                          Position = (byte)aoMesh.Position,
                                          Id = (uint)aoMesh.Mesh,
                                          OverrideTextureId = aoMesh.OverrideTexture,
                                          Layer = (byte)aoMesh.Layer
                                      }).ToArray();

            // And a mesh of nothing is not a mesh. The captures give a monster
            // an empty list - its shape is its monster data - where OmniCell
            // sent one entry whose id was zero, for every spawn in Arete
            // Landing, because MeshLayers had no equipment to build one from.
            scfu.Meshes = built.All(mesh => mesh.Id == 0) ? new Mesh[0] : built;

            // End Meshs

            // The serializer preserves this bit rather than deciding it,
            // because nineteen of the 3,938 captured characters go without it
            // and no rule separates them. Everything the live server sends
            // otherwise has it, so everything this server sends has it too.
            scfu.Flags |= SimpleCharFullUpdateFlags.UnknownFlag;

            scfu.Flags2 = 0; // packetFlags2
            scfu.Unknown2 = 0;

            return scfu;
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <returns>
        /// </returns>
        public static SimpleCharFullUpdateMessage ConstructMessage(IZoneClient client)
        {
            return ConstructMessage((Character)client.Controller.Character);
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="receiver">
        /// </param>
        public static void SendToOne(ICharacter character, IZoneClient receiver)
        {
            SimpleCharFullUpdateMessage message = ConstructMessage((Character)character);
            receiver.Controller.Character.Send(message);
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        public static void SendToPlayfield(IZoneClient client)
        {
            SimpleCharFullUpdateMessage message = ConstructMessage(client);
            client.Controller.Character.Playfield.Announce(message);
        }


        #endregion
    }
}