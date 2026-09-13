#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Functions.GameFunctions
{
    #region Usings ...

    using OmniCell.Core.Entities;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using MsgPack;

    #endregion

    /// <summary>
    /// Changes a skill by an amount.
    /// </summary>
    /// <remarks>
    /// The second most used function in nanos.dat after Modify - 2304 calls -
    /// and nothing implemented it, so every nano that raises a skill did
    /// nothing at all once casting started working.
    ///
    /// It is called the same way Modify is, with a stat and an amount, and a
    /// sweep of all 2304 calls finds no other shape. Weapon Augmentation is the
    /// clearest example: seven calls, +2 each, to projectile, melee, energy,
    /// chemical, radiation, cold and poison damage modifiers, which is exactly
    /// what that nano does in the game.
    ///
    /// So this does what Modify does. Whether Anarchy Online distinguishes the
    /// two beyond which list the client shows the change in is not visible from
    /// the data - both take a stat and an amount, and both are undone by the
    /// nano's OnTerminate, which is what makes a buff a buff. If a difference
    /// ever turns up, it belongs here and in modify.cs and nowhere else.
    /// </remarks>
    internal class skill : FunctionPrototype
    {
        #region Constants

        private const FunctionType functionId = FunctionType.Skill;

        #endregion

        #region Public Properties

        public override FunctionType FunctionId
        {
            get
            {
                return functionId;
            }
        }

        #endregion

        #region Public Methods and Operators

        public override bool Execute(
            INamedEntity self,
            IEntity caller,
            IInstancedEntity target,
            MessagePackObject[] arguments)
        {
            var character = self as Character;
            if (character == null || arguments == null || arguments.Length < 2)
            {
                return false;
            }

            lock (target)
            {
                character.Stats[arguments[0].AsInt32()].Modifier += arguments[1].AsInt32();
            }

            return true;
        }

        #endregion
    }
}
