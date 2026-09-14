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

namespace OmniCell.Core.NPCHandler
{
    using OmniCell.Core.Textures;

    #region Usings ...

    using System;
    using System.Collections.Generic;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Vector;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using Quaternion = OmniCell.Core.Vector.Quaternion;

    #endregion

    public static class NonPlayerCharacterHandler
    {
        public static Character SpawnMobFromTemplate(
            string hash,
            Identity playfieldIdentity,
            Coordinate coord,
            Quaternion heading,
            IController controller,
            int desiredLevel = -1)
        {
            Character mobCharacter = null;

            DBMobTemplate mob = MobTemplateDao.Instance.GetMobTemplateByHash(hash);
            if (mob != null)
            {
                int level = desiredLevel;
                if (level == -1)
                {
                    // Get random inside level range
                    Random rnd = new Random();
                    level = mob.MinLvl + rnd.Next(mob.MaxLvl - mob.MinLvl);
                }
                else
                {
                    // Put it inside level range
                    level = (level < mob.MinLvl ? mob.MinLvl : level);
                    level = level > mob.MaxLvl ? mob.MaxLvl : level;
                }
                mobCharacter = CreateMob(mob, playfieldIdentity, coord, heading, controller, level);
            }
            return mobCharacter;
        }

        private static Character CreateMob(
            DBMobTemplate mob,
            Identity playfieldIdentity,
            Coordinate coord,
            Quaternion heading,
            IController controller,
            int level)
        {
            IPlayfield playfield = Pool.Instance.GetObject<IPlayfield>(Identity.None, playfieldIdentity);
            if (playfield != null)
            {
                int newInstanceId = Pool.Instance.GetFreeInstance<Character>(1000000, IdentityType.CanbeAffected);
                Identity newIdentity = new Identity() { Type = IdentityType.CanbeAffected, Instance = newInstanceId };
                Character mobCharacter = new Character(playfield.Identity, newIdentity, controller);
                mobCharacter.Read();
                mobCharacter.Coordinates(coord);
                mobCharacter.Playfield = Pool.Instance.GetObject<IPlayfield>(Identity.None, playfieldIdentity);
                mobCharacter.RawHeading = new Quaternion(heading.xf, heading.yf, heading.zf, heading.wf);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.life, (uint)mob.Health);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.level, (uint)level);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.npcfamily, (uint)mob.NPCFamily);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.side, (uint)mob.Side);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.fatness, (uint)mob.Fatness);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.breed, (uint)mob.Breed);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.sex, (uint)mob.Sex);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.race, (uint)mob.Race);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.flags, (uint)mob.Flags);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.monsterdata, (uint)mob.MonsterData);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.monsterscale, (uint)mob.MonsterScale);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.profession, 15);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.accountflags, 0);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.expansion, 0);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.runspeed, 513);
                mobCharacter.Stats[StatIds.headmesh].BaseValue = (uint)mob.HeadMesh;
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.losheight, 15);
                mobCharacter.Stats.SetBaseValueWithoutTriggering((int)StatIds.visualprofession, 15);

                /*
                // For testing only, blue trousers and a helmet
                IItem trousers = new Item(1, ItemLoader.ItemList[27350].GetLowId(1),ItemLoader.ItemList[27350].GetLowId(1));
                IItem helmet = new Item(1, ItemLoader.ItemList[85534].GetLowId(1), ItemLoader.ItemList[85534].GetHighId(1));
                mobCharacter.BaseInventory.Pages[(int)IdentityType.ArmorPage].Add((int)ArmorSlots.Legs + mobCharacter.BaseInventory.Pages[(int)IdentityType.ArmorPage].FirstSlotNumber, trousers);
                mobCharacter.BaseInventory.Pages[(int)IdentityType.ArmorPage].Add((int)ArmorSlots.Head + mobCharacter.BaseInventory.Pages[(int)IdentityType.ArmorPage].FirstSlotNumber, helmet);
                */

                // Set the MeshLayers correctly ( Head mesh!! )  /!\
                // TODO: This needs to be in StatHeadmesh.cs (somehow)
                mobCharacter.MeshLayer.SetMesh(0, mob.HeadMesh, 0, MeshLayers.BaseHeadLayer);
                mobCharacter.SocialMeshLayer.SetMesh(0, mob.HeadMesh, 0, MeshLayers.BaseHeadLayer);

                mobCharacter.Name = mob.Name;
                mobCharacter.FirstName = "";
                mobCharacter.LastName = "";
                controller.Character = mobCharacter;
                return mobCharacter;
            }
            return null;
        }

        public static ICharacter InstantiateMobSpawn(
            DBMobSpawn mob,
            DBMobSpawnStat[] stats,
            IController npccontroller,
            IPlayfield playfield)
        {
            if (playfield != null)
            {
                Identity mobId = new Identity() { Type = IdentityType.CanbeAffected, Instance = mob.Id };
                if (Pool.Instance.GetObject(playfield.Identity, mobId) != null)
                {
                    throw new Exception("Object " + mobId.ToString(true) + " already exists!!");
                }
                Character cmob = new Character(playfield.Identity, mobId, npccontroller);
                cmob.Read();
                cmob.Playfield = playfield;
                cmob.Coordinates(new Coordinate() { x = mob.X, y = mob.Y, z = mob.Z });
                cmob.RawHeading = new Quaternion(mob.HeadingX, mob.HeadingY, mob.HeadingZ, mob.HeadingW);
                cmob.Name = mob.Name;
                cmob.FirstName = "";
                cmob.LastName = "";
                foreach (DBMobSpawnStat stat in stats)
                {
                    cmob.Stats.SetBaseValueWithoutTriggering(stat.Stat, (uint)stat.Value);
                }

                cmob.Stats.SetBaseValueWithoutTriggering((int)StatIds.visualprofession, cmob.Stats[StatIds.profession].BaseValue);
                // initiate affected stats calculation
                int temp = cmob.Stats[StatIds.level].Value;
                temp = cmob.Stats[StatIds.agility].Value;
                temp = cmob.Stats[StatIds.headmesh].Value;
                cmob.MeshLayer.SetMesh(0, cmob.Stats[StatIds.headmesh].Value, 0, MeshLayers.BaseHeadLayer);
                cmob.SocialMeshLayer.SetMesh(0, cmob.Stats[StatIds.headmesh].Value, 0, MeshLayers.BaseHeadLayer);

                // What the character is wearing.
                //
                // The five texture columns have been in the spawn table all
                // along and nothing ever read them, so every spawned character
                // went out with five textures whose ids were zero - which is why
                // a Dockworker in the captures wears 295555, 295553, 295554,
                // 295552 and 295556 and ours wore nothing at all.
                //
                // Zero is a real value here and not an absence: Bodyguard Logan
                // Fixx is sent by the live server with five zero-id textures.
                // That is the trap this walked into once before, from the other
                // side - the textures were dropped for being all zero, when the
                // answer was to fill them in.
                cmob.Textures.Clear();
                cmob.Textures.Add(new AOTextures(0, mob.Textures0));
                cmob.Textures.Add(new AOTextures(1, mob.Textures1));
                cmob.Textures.Add(new AOTextures(2, mob.Textures2));
                cmob.Textures.Add(new AOTextures(3, mob.Textures3));
                cmob.Textures.Add(new AOTextures(4, mob.Textures4));
                // A spawn with nowhere to walk stores no waypoints at all, and
                // the column stays null. Reading .ToArray() off that threw, so
                // any playfield holding one stationary mob failed to load.
                List<MobSpawnWaypoint> waypoints = mob.Waypoints == null || mob.Waypoints.Length == 0
                                                       ? new List<MobSpawnWaypoint>()
                                                       : MessagePackZip.DeserializeData<MobSpawnWaypoint>(
                                                           mob.Waypoints);
                foreach (MobSpawnWaypoint wp in waypoints)
                {
                    Waypoint mobwp = new Waypoint();
                    mobwp.Position.x = wp.X;
                    mobwp.Position.y = wp.Y;
                    mobwp.Position.z = wp.Z;
                    mobwp.Running = wp.WalkMode == 1;
                    cmob.Waypoints.Add(mobwp);
                }
                npccontroller.Character = cmob;
                if (cmob.Waypoints.Count > 2)
                {
                    cmob.Controller.State = CharacterState.Patrolling;
                }
                cmob.DoNotDoTimers = false;
                return cmob;
            }
            return null;
        }
    }
}