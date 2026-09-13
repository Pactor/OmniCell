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
    /// The other pet summon.
    /// </summary>
    /// <remarks>
    /// Named SpawnItem in the function list, but every one of its 476 calls in
    /// nanos.dat summons a pet, and it is called exactly the way SummonPet is: a
    /// four character template hash, a level, and a flag.
    ///
    /// The hashes give it away. ENAU, ENGA, ENGU, ENWA and EGWA are the
    /// engineer's automatons, guards and warriors; BUWO is the bureaucrat's
    /// Limited Worker-Droid, which is what a capture of a low level bureaucrat
    /// casting nano 46362 shows going out.
    ///
    /// So it does what summonpet does, and the summoning itself lives on Pets
    /// rather than being written twice.
    /// </remarks>
    internal class spawnitem : FunctionPrototype
    {
        #region Constants

        private const FunctionType functionId = FunctionType.SpawnItem;

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

            // Not every SpawnItem is a pet. Packages open with it too: 303474
            // lists eight keys and live granted eight items for them, in order
            // (20260911-171203_s12 seq 1617-1631). A key a capture has tied to
            // an item is granted as one; anything else is summoned as before.
            string key = arguments[0].AsString();
            if (ItemSpawns.Knows(key))
            {
                return ItemSpawns.Grant(self as ICharacter, key, arguments[1].AsInt32());
            }

            return Pets.Summon(self as ICharacter, key, arguments[1].AsInt32());
        }

        #endregion
    }
}
