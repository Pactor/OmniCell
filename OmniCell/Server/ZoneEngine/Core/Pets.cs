#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core
{
    #region Usings

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.NPCHandler;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Packets;

    #endregion

    /// <summary>
    /// Who owns which pet.
    /// </summary>
    /// <remarks>
    /// The server had no idea. A pet was spawned as an ordinary NPC and AddPet
    /// told the client whose it was, but nothing on this side remembered, so
    /// there was no way to answer "is this yours" - which is the first question
    /// a pet command has to ask, and the only thing standing between a working
    /// pet bar and one player commanding another's pets.
    ///
    /// Kept here rather than on ICharacter for the same reason fights and casts
    /// are: adding it should not change an interface implemented across the
    /// whole server.
    /// </remarks>
    public static class Pets
    {
        #region Fields

        /// <summary>
        /// Pets, by the identity of whoever summoned them.
        /// </summary>
        private static readonly ConcurrentDictionary<Identity, List<Identity>> Owned =
            new ConcurrentDictionary<Identity, List<Identity>>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Summons a pet for a character.
        /// </summary>
        /// <remarks>
        /// Two different game functions summon pets and they are called
        /// identically: SummonPet, 53167, and SpawnItem, 53064. All 476 calls to
        /// the second have the same three arguments as the first - a four
        /// character template hash, a level, and a flag - and the hashes are pet
        /// names: ENAU, ENGA, ENGU, ENWA for engineer automatons and guards,
        /// BUWO for the bureaucrat's Limited Worker-Droid.
        ///
        /// So the difference between them is not what they do, and the summoning
        /// lives here rather than in either function.
        ///
        /// What the client is told is two messages, from a capture of a
        /// metaphysicist summoning three: the pet described as an ordinary
        /// character, then AddPet saying whose it is, in that order every time.
        /// </remarks>
        public static bool Summon(ICharacter master, string hash, int level)
        {
            if (master == null || master.Playfield == null || string.IsNullOrEmpty(hash))
            {
                return false;
            }

            var controller = new NPCController();
            Character pet;

            try
            {
                pet = NonPlayerCharacterHandler.SpawnMobFromTemplate(
                    hash,
                    master.Playfield.Identity,
                    master.Coordinates(),
                    master.RawHeading,
                    controller,
                    level);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Could not summon pet from template \"" + hash + "\": " + ex.GetType().Name + ": "
                    + ex.Message);
                return false;
            }

            if (pet == null)
            {
                Console.WriteLine(
                    "Nano wanted to summon template \"" + hash + "\", which is not in mobtemplate. "
                    + "Almost no pet templates are - see the remarks on Pets.Summon.");
                return false;
            }

            pet.Playfield = master.Playfield;

            SimpleCharFullUpdateMessage spawned = SimpleCharFullUpdate.ConstructMessage(pet);
            master.Playfield.Announce(spawned);
            AppearanceUpdateMessageHandler.Default.Send(pet);
            AddPetMessageHandler.Default.Send(master, pet.Identity);

            Add(master.Identity, pet.Identity);
            controller.Follow(master.Identity);

            return true;
        }

        /// <summary>
        /// Records a newly summoned pet.
        /// </summary>
        public static void Add(Identity master, Identity pet)
        {
            Owned.AddOrUpdate(
                master,
                id => new List<Identity> { pet },
                (id, list) =>
                    {
                        lock (list)
                        {
                            if (!list.Contains(pet))
                            {
                                list.Add(pet);
                            }
                        }

                        return list;
                    });
        }

        /// <summary>
        /// Forgets one pet.
        /// </summary>
        public static void Remove(Identity master, Identity pet)
        {
            List<Identity> list;
            if (!Owned.TryGetValue(master, out list))
            {
                return;
            }

            lock (list)
            {
                list.Remove(pet);
            }
        }

        /// <summary>
        /// Everything this character has summoned.
        /// </summary>
        public static IEnumerable<Identity> Of(Identity master)
        {
            List<Identity> list;
            if (!Owned.TryGetValue(master, out list))
            {
                return new Identity[0];
            }

            lock (list)
            {
                return list.ToArray();
            }
        }

        /// <summary>
        /// Whether this pet belongs to this character.
        /// </summary>
        public static bool Owns(Identity master, Identity pet)
        {
            return Of(master).Contains(pet);
        }

        /// <summary>
        /// Drops everything a character owned, when it leaves.
        /// </summary>
        public static void Forget(Identity master)
        {
            List<Identity> ignored;
            Owned.TryRemove(master, out ignored);
        }

        #endregion
    }
}
