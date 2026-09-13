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

namespace LoginEngine.Packets
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Text;

    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// </summary>
    public class CharacterName
    {
        #region Static Fields

        /// <summary>
        /// </summary>
        private static string mandatoryVowel = "aiueo"; /* 5 chars */

        /// <summary>
        /// </summary>
        private static string optionalOrdCon = "vybcfghjqktdnpmrlws"; /* 19 chars */

        /// <summary>
        /// </summary>
        private static string optionalOrdEnd = "nmrlstyzx"; /* 9 chars */

        #endregion

        #region Public Properties

        /// <summary>
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// </summary>
        public int Breed { get; set; }

        /// <summary>
        /// </summary>
        public int Fatness { get; set; }

        /// <summary>
        /// </summary>
        public int Gender { get; set; }

        /// <summary>
        /// </summary>
        public int HeadMesh { get; set; }

        /// <summary>
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// </summary>
        public int MonsterScale { get; set; }

        /// <summary>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// </summary>
        public int Profession { get; set; }

        #endregion

        #region Properties

        /// <summary>
        /// </summary>
        private int[] Abis { get; set; }

        #endregion

        #region Constants

        // Professions, as the client and the profession stat number them.
        private const int Soldier = 1;

        private const int MartialArtist = 2;

        private const int Engineer = 3;

        private const int Fixer = 4;

        private const int Agent = 5;

        private const int Adventurer = 6;

        private const int Trader = 7;

        private const int Bureaucrat = 8;

        private const int Enforcer = 9;

        private const int Doctor = 10;

        private const int NanoTechnician = 11;

        private const int MetaPhysicist = 12;

        private const int Keeper = 14;

        private const int Shade = 15;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public int CheckAgainstDatabase()
        {
            /* name in use */
            if (CharacterDao.Instance.ExistsByName(this.Name))
            {
                return 0;
            }

            return this.CreateNewChar();
        }

        /// <summary>
        /// </summary>
        /// <param name="charid">
        /// </param>
        public void DeleteChar(int charid)
        {
            try
            {
                CharacterDao.Instance.Delete(charid);
            }
            catch (Exception e)
            {
                Console.WriteLine(this.Name + e.Message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="profession">
        /// </param>
        /// <returns>
        /// </returns>
        public string GetRandomName(Profession profession)
        {
            var random = new Random();
            byte randomNameLength = 0;
            var randomLength = (byte)random.Next(3, 8);
            var sb = new StringBuilder();
            while (randomNameLength <= randomLength)
            {
                if (random.Next(14) > 4)
                {
                    sb.Append(optionalOrdCon.Substring(random.Next(0, 18), 1));
                    randomNameLength++;
                }

                sb.Append(mandatoryVowel.Substring(random.Next(0, 4), 1));
                randomNameLength++;

                if (random.Next(14) <= 4)
                {
                    continue;
                }

                sb.Append(optionalOrdEnd.Substring(random.Next(0, 8), 1));
                randomNameLength++;
            }

            string name = sb.ToString();
            name = char.ToUpper(name[0]) + name.Substring(1);
            return name;
        }

        /// <summary>
        /// </summary>
        /// <param name="startInSL">
        /// </param>
        /// <param name="charid">
        /// </param>
        public void SendNameToStartPlayfield(bool startInSL, int charid)
        {
            int playfield, x, y, z;

            if (startInSL)
            {
                playfield = 4001;
                x = 850;
                y = 43;
                z = 565;
            }
            else
            {
                // Arete Landing, the tutorial playfield a new character has
                // started in since 18.4. Taken from a live capture of a
                // character created on 2026-09-09: PlayfieldAnarchyF named
                // playfield 6553 and put the character down at these
                // coordinates before it had moved.
                //
                // The old value here was 4582 at 939,20,732, which is the
                // newbie island the game stopped using years ago. A client
                // that is sent there loads it happily, which is why nobody
                // noticed - it is simply the wrong place.
                playfield = 6553;
                x = 3609;
                y = 52;
                z = 786;
            }

            DBCharacter character = CharacterDao.Instance.Get(charid);
            if (character != null)
            {
                CharacterDao.Instance.Save(
                    character // woo....
                    ,
                    new { Id = charid, Playfield = playfield, X = x, Y = y, Z = z });

                CharacterDao.Instance.SetPlayfield(charid, (int)IdentityType.Playfield, playfield);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <returns>
        /// charID
        /// </returns>
        private int CreateNewChar()
        {
            DBCharacter newCharacter = new DBCharacter
                                       {
                                           FirstName = string.Empty,
                                           LastName = string.Empty,
                                           Name = this.Name,
                                           Username = this.AccountName,
                                       };

            CharacterDao.Instance.Add(newCharacter);

            int charID = newCharacter.Id;

            #region Statistics

            switch (this.Breed)
            {
                case 0x1: /* solitus */
                    this.Abis = new[] { 6, 6, 6, 6, 6, 6 };
                    break;
                case 0x2: /* opifex */
                    this.Abis = new[] { 3, 3, 10, 6, 6, 15 };
                    break;
                case 0x3: /* nanomage */
                    this.Abis = new[] { 3, 10, 6, 15, 3, 3 };
                    break;
                case 0x4: /* atrox */
                    this.Abis = new[] { 15, 3, 3, 3, 10, 6 };
                    break;
                default:
                    Console.WriteLine("unknown breed: {0}", this.Breed);
                    break;
            }

            List<DBStats> stats = new List<DBStats>();

            // Transmit GM level into stats table
            stats.Add(
                new DBStats
                {
                    Type = 50000,
                    Instance = charID,
                    StatId = 215,
                    StatValue = LoginDataDao.Instance.GetByUsername(this.AccountName).GM
                });

            // Flags
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 0, StatValue = 0x00081241 });

            // Level
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 54, StatValue = 1 });

            // SEXXX
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 59, StatValue = this.Gender });

            // Headmesh
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 64, StatValue = this.HeadMesh });

            // MonsterScale
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 360, StatValue = this.MonsterScale });

            // Visual Sex (even better ^^)
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 369, StatValue = this.Gender });

            // Breed
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 4, StatValue = this.Breed });

            // Visual Breed
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 367, StatValue = this.Breed });

            // Profession / 60
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 60, StatValue = this.Profession });

            // VisualProfession / 368
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 368, StatValue = this.Profession });

            // Fatness / 47
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 47, StatValue = this.Fatness });

            // Strength / 16
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 16, StatValue = this.Abis[0] });

            // Psychic / 21
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 21, StatValue = this.Abis[1] });

            // Sense / 20
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 20, StatValue = this.Abis[2] });

            // Intelligence / 19
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 19, StatValue = this.Abis[3] });

            // Stamina / 18
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 18, StatValue = this.Abis[4] });

            // Agility / 17
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 17, StatValue = this.Abis[5] });

            // Health and nano, left at zero on purpose.
            //
            // These used to be written as literal 1, and that is what the
            // character then walked into the world with: one hit point and one
            // point of nano. The captures show a level 1 in Arete Landing at
            // 38 of 38 health and 31 of 31 nano - full, as anyone would expect
            // of a character that has not done anything yet.
            //
            // The maximum is not a number that can be written here. It comes out
            // of the breed and profession tables in OmniCell.Stats, against a
            // stat list this process does not build, so zero means "never set"
            // and the zone fills it to whatever the formulas say the first time
            // the character is played. See StartingKit.
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 27, StatValue = 0 });
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 214, StatValue = 0 });

            // NPCFamily / 455
            stats.Add(new DBStats { Type = 50000, Instance = charID, StatId = 455, StatValue = 0 });

            stats.Add(
                new DBStats
                {
                    Type = 50000,
                    Instance = charID,
                    StatId = 389,
                    StatValue = LoginDataDao.Instance.GetByUsername(this.AccountName).Expansions
                });

            StatDao.Instance.BulkReplace(stats);

            #endregion

            this.GiveStartingEquipment(charID);

            return charID;
        }

        /// <summary>
        /// What a new character walks into Arete Landing carrying.
        /// </summary>
        /// <remarks>
        /// Three parts, from three different places, and it is worth saying
        /// which is which.
        ///
        /// The kit everybody gets is from the captures. Three Arete Landing
        /// sessions hold a whole inventory - a bureaucrat and two of a fixer -
        /// and five items appear in all three. Those five.
        ///
        /// The weapon is from the item database. Every profession has weapons
        /// only it may hold, and the skills those weapons use are the skills it
        /// fights with; counting them across 120,569 items gives one clear
        /// answer per profession. Two of those answers can be checked against
        /// the captures - the fixer with a submachine gun and the bureaucrat
        /// with a pistol - and both agree, which is the reason to trust the
        /// other twelve.
        ///
        /// The weapons themselves are the ones a level 1 can actually hold: QL 1,
        /// and carrying no requirement of any kind. They turn out to be one
        /// family of consecutive ids - Worn Dagger, Worn Blade, Worn Sword, the
        /// Adventuring Pistol, Worn Hammer - alongside the Solar-Powered guns.
        ///
        /// The extras for the fixer and the bureaucrat are the rest of what
        /// those two captures show them carrying. The other twelve professions
        /// get no extras, because no capture shows what they carry and a guess
        /// dressed as data is worse than an empty slot.
        ///
        /// One thing this is not: the tutorial. In 18.8 the kit is handed over
        /// by an NPC in Arete, and until that conversation runs, creation is the
        /// nearest honest place to put it.
        ///
        /// Slot 0x40 upwards is the backpack - see PlayerInventoryPage, and the
        /// captures, which put the same items at 64, 65, 66 and on.
        /// </remarks>
        private void GiveStartingEquipment(int charID)
        {
            // lowId, quality, count
            var kit = new List<int[]>
                          {
                              new[] { 291082, 1, 50 },  // Health and Nano Recharger
                              new[] { 291043, 1, 25 },  // Health and Nano Stim
                              new[] { 252158, 1, 1 },   // Blackmane's Belt Component Platform
                              new[] { 292235, 1, 1 },   // Razor's Polarized Specs
                              new[] { 296977, 1, 1 }    // Colonist Survival Pack
                          };

            kit.Add(new[] { StartingWeapon(this.Profession), 1, 1 });

            foreach (int crystal in StartingCrystals(this.Profession))
            {
                kit.Add(new[] { crystal, 1, 1 });
            }

            switch (this.Profession)
            {
                case Fixer:
                    kit.Add(new[] { 150306, 1, 1 });    // Android NCU Upgrade
                    kit.Add(new[] { 296569, 1, 1 });    // Generic Nano Transmitter
                    kit.Add(new[] { 296570, 1, 1 });    // Blank Info Chip
                    kit.Add(new[] { 296571, 1, 1 });    // 2560-Bit Encryption Compiler
                    break;

                case Bureaucrat:
                    kit.Add(new[] { 296576, 1, 1 });    // Personalized ICC ID Chip
                    kit.Add(new[] { 297366, 1, 1 });    // Pet Cage
                    break;
            }

            int slot = 0x40;
            foreach (int[] entry in kit)
            {
                ItemDao.Instance.Add(
                    new DBItem
                        {
                            // Not the other way round, however it reads. An
                            // inventory page identifies itself as the character
                            // in the type and the page number in the instance -
                            // see BaseInventoryPage's constructor - so that is
                            // what it looks its contents up by, and rows written
                            // the way round that sounds right are simply never
                            // found.
                            containertype = charID,
                            containerinstance = (int)IdentityType.Inventory,
                            containerplacement = slot++,
                            lowid = entry[0],
                            highid = entry[0],
                            quality = entry[1],
                            multiplecount = entry[2]
                        });
            }
        }

        /// <summary>
        /// The nano crystals a profession starts with.
        /// </summary>
        /// <remarks>
        /// These name themselves. Nineteen items in the database are called
        /// "&lt;Profession&gt;: Startup Crystal - &lt;Nano&gt;", one or two per
        /// profession, and between them they cover all twelve of the
        /// professions that existed before Shadowlands.
        ///
        /// A name is not evidence on its own, so the set was found a second way
        /// that has nothing to do with names: every item of quality 1 that
        /// uploads a nano (function 53019), is locked to a single profession,
        /// and asks for nothing beyond a nano skill a new character already
        /// has. Twenty-one items come back, and nineteen of them are the same
        /// nineteen. The two that are not are a stray Agent crystal and an item
        /// whose name begins "Test Item:". Two definitions with no overlap
        /// picking out the same set is the reason to believe it.
        ///
        /// Agent's Shadow Veil is the one member of the named family the
        /// mechanical test rejects: it wants Matter Creation 9 and Biological
        /// Metamorphosis 7 where a fresh character has 5. It is still issued -
        /// it is a crystal to grow a little into, which is ordinary enough - so
        /// it stays.
        ///
        /// Keeper and Shade have no startup crystal, because the family predates
        /// them by an expansion. They get the one nano each that the same
        /// mechanical test finds for them - Keeper's Edge and Whetstone Effect,
        /// both quality 1, both locked to the profession, both castable
        /// immediately. That is the closest thing to an issued crystal that
        /// exists for either.
        ///
        /// The lock is not always on the same stat, which is worth knowing
        /// before extending this. Some crystals lock on profession (60) and
        /// some on visualprofession (368), and a search for one alone finds
        /// roughly half of them - the twelve old professions mostly use 368 and
        /// the Shadowlands two use 60.
        ///
        /// The two captured kits are no help here. Both characters had levelled
        /// and their crystals are things they went and got: quality 25
        /// composites a level 1 could not cast if it wanted to.
        /// </remarks>
        private static int[] StartingCrystals(int profession)
        {
            switch (profession)
            {
                case Soldier:         return new[] { 29092 };           // Body Boost
                case MartialArtist:   return new[] { 43382 };           // Lesser Controlled Rage
                case Engineer:        return new[] { 43329, 29194 };    // Feeble Automaton, Swift Weapon
                case Fixer:           return new[] { 43380 };           // Minor Suppressor
                case Agent:           return new[] { 43383, 29191 };    // Minor Nano Augmentation, Shadow Veil
                case Adventurer:      return new[] { 28742 };           // Quick Heal
                case Trader:          return new[] { 43378, 78417 };    // Weak Delayed Health Payment, Weak Health Funnel
                case Bureaucrat:      return new[] { 43381, 29625 };    // Momentary Daze, Winter's Bite
                case Enforcer:        return new[] { 43379 };           // Thug's Delight
                case Doctor:          return new[] { 28778, 43384 };    // Improved Healing, Prototype Biotoxin
                case NanoTechnician:  return new[] { 28815, 42111 };    // Ice Flechette, Malaise of Movement
                case MetaPhysicist:   return new[] { 29193 };           // Mind Pain
                case Keeper:          return new[] { 210298 };          // Keeper's Edge
                case Shade:           return new[] { 211155 };          // Whetstone Effect

                default:
                    // A crystal locked to a profession this does not know about
                    // would be a crystal nobody can use. Better none.
                    return new int[0];
            }
        }

        /// <summary>
        /// The weapon a profession starts with.
        /// </summary>
        /// <remarks>
        /// Each line is a profession, the weapon skill its own locked weapons
        /// use, and how many of them said so. Counted over the whole item
        /// database; the two the captures can check both agree.
        ///
        /// Engineer was the one close call: grenade 40 against pistol 25. The
        /// count wins, as it does everywhere else here.
        /// </remarks>
        private static int StartingWeapon(int profession)
        {
            switch (profession)
            {
                case Soldier:         return 121569;  // assaultrifle    (77)  Solar-Powered Assault Rifle
                case MartialArtist:   return 218395;  // piercing        (39)  Worn Dagger
                case Engineer:        return 248338;  // grenade         (40)  Solar-Powered Grenade Launcher
                case Fixer:           return 121571;  // submachinegun   (54)  Solar-Powered Submachine Gun
                case Agent:           return 121568;  // rifle           (52)  Solar-Powered Rifle
                case Adventurer:      return 218405;  // pistol         (119)  Solar-Powered Adventuring Pistol
                case Trader:          return 121570;  // shotgun         (41)  Solar-Powered Shotgun
                case Bureaucrat:      return 121567;  // pistol          (22)  Solar-Powered Pistol
                case Enforcer:        return 218403;  // twohandedged    (54)  Worn Blade
                case Doctor:          return 121567;  // pistol          (15)  Solar-Powered Pistol
                case NanoTechnician:  return 121567;  // pistol          (14)  Solar-Powered Pistol
                case MetaPhysicist:   return 218404;  // onehandedged    (14)  Worn Sword
                case Keeper:          return 218403;  // twohandedged   (102)  Worn Blade
                case Shade:           return 218395;  // piercing        (47)  Worn Dagger

                default:
                    // Nobody else exists, but a character with a profession this
                    // does not know about gets the pistol rather than nothing:
                    // an empty hand is harder to notice than a wrong weapon.
                    return 121567;
            }
        }

        #endregion
    }
}