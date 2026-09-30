#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Functions.GameFunctions
{
    #region Usings ...

    using MsgPack;

    using OmniCell.Core.Entities;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using ZoneEngine.Core.Combat;

    #endregion

    /// <summary>
    /// The wait before a perk can be used again.
    /// </summary>
    /// <remarks>
    /// Every perk action item carries one of these on the event that runs when
    /// the perk is pressed, and it is where the cooldown lives:
    ///
    ///     LockPerk(AttackSpeed, 778, 95)     Seppuku Slash
    ///     LockPerk(AttackSpeed, 190, 40)     Lay On Hands
    ///
    /// The second argument is the perk's short id and the third is the wait in
    /// seconds. Both match the retail recording exactly - the server sent
    /// <see cref="CharacterActionType.PerkUnavailable"/> with those very
    /// numbers, and each perk came back within a second and a half of them.
    ///
    /// The first argument is a stat - AttackSpeed, stat 3 - on every perk in
    /// the pack. Nothing is done with it. No stat changed on the player when a
    /// perk was used in 48 minutes of recording, so writing to it would be
    /// inventing traffic retail does not send; the cooldown is told by the
    /// pair of CharacterActions and nothing else.
    ///
    /// The lock itself, and the message when it ends, are
    /// <see cref="Perks.Lock"/>.
    /// </remarks>
    internal class lockperk : FunctionPrototype
    {
        public override FunctionType FunctionId
        {
            get
            {
                return FunctionType.LockPerk;
            }
        }

        public override bool Execute(
            INamedEntity self,
            IEntity caller,
            IInstancedEntity target,
            MessagePackObject[] arguments)
        {
            var character = self as Character;
            if (character == null || arguments == null || arguments.Length < 3)
            {
                return false;
            }

            int perk = arguments[1].AsInt32();
            int seconds = arguments[2].AsInt32();
            if (perk <= 0 || seconds <= 0)
            {
                return false;
            }

            Perks.Lock(character, perk, seconds);
            return true;
        }
    }
}
