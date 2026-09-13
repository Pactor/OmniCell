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

    using System;

    using OmniCell.Core.Entities;
    using OmniCell.Core.NPCHandler;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using MsgPack;

    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core;
    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Packets;

    #endregion

    /// <summary>
    /// Summons a pet.
    /// </summary>
    /// <remarks>
    /// A summon nano calls this with three arguments, and a probe over
    /// nanos.dat shows the same three every time:
    ///
    ///     [0] a four character template hash, "PT56" or "PT50"
    ///     [1] a level
    ///     [2] -1
    ///
    /// The hash is a mobtemplate hash, which is what SpawnMobFromTemplate
    /// already takes, so the spawning itself needs nothing new.
    ///
    /// What the client is then told is two messages, and a capture of a
    /// metaphysicist summoning Mortificant the Eternal, Yidira and Zhok the
    /// Abomination shows the pair three times in that order: the pet described
    /// as an ordinary character, then AddPet saying whose it is.
    ///
    /// One thing that will stop this working, and is not this function's fault:
    /// mobtemplate holds 168 templates and not one of them is a pet - no hash
    /// begins with PT. Whatever rdbreader read the templates out of did not
    /// carry them, the same way it did not carry the newbie tradeskill recipes.
    /// A summon with no template says so and does nothing, rather than
    /// pretending to have worked.
    /// </remarks>
    internal class summonpet : FunctionPrototype
    {
        #region Constants

        private const FunctionType functionId = FunctionType.SummonPet;

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
            if (arguments == null || arguments.Length < 2)
            {
                return false;
            }

            return Pets.Summon(self as ICharacter, arguments[0].AsString(), arguments[1].AsInt32());
        }

        #endregion
    }
}
