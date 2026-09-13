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

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Network;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;

    using ZoneEngine.Core.Combat;
    using ZoneEngine.Core.Controllers;

    #endregion

    /// <summary>
    /// Telling a pet what to do.
    /// </summary>
    /// <remarks>
    /// Goes both ways: the client sends one to command its pets, and the server
    /// sends the same message back. 149 up and 34 down across the captures.
    ///
    /// Unknown2 is the command, and a capture taken with each button marked as
    /// it was pressed says what the numbers are:
    ///
    ///     1   follow      one message per pet, three pets, three messages
    ///     4   wait        the same, one per pet
    ///     7   attack      names the pet and no target
    ///    10   terminate   Unknown1 set to 1 and no pet named at all
    ///    12   heal        in the window marked "pet heal"
    ///
    /// Two of them carry less than you would expect, and both for the same
    /// reason - the server already knows.
    ///
    /// Attack names the pet and not what to attack. The capture shows why: the
    /// master sent an AttackMessage of its own at the same moment, against the
    /// mob it had targeted. The pet is being told to attack whatever its master
    /// is attacking, so the target does not need sending twice.
    ///
    /// Terminate names nobody at all. It dismisses everything the sender owns,
    /// which is why it needs the ownership record rather than a list from the
    /// client.
    ///
    /// The ownership check is not decoration. Without it, the identities in this
    /// message are whatever the client chose to put there, and one player could
    /// command another's pets.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class PetCommandMessageHandler : BaseMessageHandler<PetCommandMessage, PetCommandMessageHandler>
    {
        #region Fields

        /// <summary>
        /// Follow the master.
        /// </summary>
        public const int Follow = 1;

        /// <summary>
        /// Stay where you are.
        /// </summary>
        public const int Wait = 4;

        /// <summary>
        /// Attack whatever the master is attacking. Names no target.
        /// </summary>
        public const int Attack = 7;

        /// <summary>
        /// Dismiss every pet the sender owns. Names no pet.
        /// </summary>
        public const int Terminate = 10;

        /// <summary>
        /// Heal the pet. Nothing is done with it yet - it wants the healing
        /// nano the profession casts, not a command of its own.
        /// </summary>
        public const int Heal = 12;

        #endregion

        #region Inbound

        /// <summary>
        /// A character is commanding its pets.
        /// </summary>
        protected override void Read(PetCommandMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            ICharacter master = client.Controller.Character;

            // Terminate names nobody. It applies to everything the sender owns,
            // which is why the list comes from here rather than from the client.
            if (message.Command == Terminate)
            {
                Dismiss(master);
                return;
            }

            foreach (Identity petIdentity in message.Pets ?? new Identity[0])
            {
                if (!Pets.Owns(master.Identity, petIdentity))
                {
                    Console.WriteLine(
                        "Character " + master.Identity.Instance + " tried to command "
                        + petIdentity.Instance + ", which is not its pet.");
                    continue;
                }

                var pet = Pool.Instance.GetObject<ICharacter>(master.Playfield.Identity, petIdentity);
                if (pet == null)
                {
                    continue;
                }

                switch (message.Command)
                {
                    case Follow:
                        ((NPCController)pet.Controller).Follow(master.Identity);
                        break;

                    case Wait:
                        pet.Controller.StopMovement();
                        break;

                    case Attack:
                        if (master.FightingTarget.Instance == 0)
                        {
                            Console.WriteLine(
                                "Character " + master.Identity.Instance
                                + " told a pet to attack while fighting nothing.");
                            break;
                        }

                        Combat.Start(pet, master.FightingTarget);
                        break;

                    default:
                        Console.WriteLine(
                            "Pet command " + message.Command + " is not implemented - see "
                            + "PetCommandMessageHandler.");
                        break;
                }
            }

            // Relayed either way. The live server echoes the command, and a
            // client that hears nothing back leaves its pet bar lit up.
            this.Send(master, message.Command, message.Pets);
        }

        /// <summary>
        /// Sends every one of a character's pets away.
        /// </summary>
        /// <remarks>
        /// RemovePet then Despawn, once each per pet, which is the order the
        /// live server uses - the pet stops being anybody's before it stops
        /// existing.
        /// </remarks>
        public static void Dismiss(ICharacter master)
        {
            foreach (Identity pet in Pets.Of(master.Identity))
            {
                RemovePetMessageHandler.Default.Send(master, pet);
                master.Playfield.Despawn(pet);
                Pets.Remove(master.Identity, pet);
            }
        }

        #endregion

        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter master, int command, IEnumerable<Identity> pets)
        {
            this.Send(master, Filler(master, command, pets), true);
        }

        private static MessageDataFiller Filler(ICharacter master, int command, IEnumerable<Identity> pets)
        {
            return message =>
            {
                message.Identity = master.Identity;
                message.Unknown = 0;
                message.Scope = command == Terminate ? 1 : 0;
                message.Command = command;
                message.CommandParameter = 0;
                message.Pets = (pets ?? new Identity[0]).ToArray();
                message.IsPetType15 = 0;
                message.CommandText = string.Empty;
            };
        }

        #endregion
    }
}
