#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Functions.GameFunctions
{
    #region Usings ...

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using MsgPack;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// The item using itself up.
    /// </summary>
    /// <remarks>
    /// There was no handler for this. Packages 303474 and 303475 end their OnUse
    /// with it, after their SpawnItems, and the live server answers opening them
    /// with CharacterAction DeleteItem on the package's slot after the granted
    /// items and before the GenericCmd acknowledgement (20260911-171203_s12 seq
    /// 1605 and 1633). The slot is the one the item was used from, which the
    /// caller appends to the arguments.
    /// </remarks>
    internal class Function_destroyitem : FunctionPrototype
    {
        #region Constants

        private const FunctionType functionId = FunctionType.DestroyItem;

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
            lock (target)
            {
                var character = self as ICharacter;
                if (character == null || arguments == null || arguments.Length == 0)
                {
                    return false;
                }

                int slot = arguments[arguments.Length - 1].AsInt32();
                IInventoryPage page = character.BaseInventory.PageFromSlot(slot);
                if (page == null || page[slot] == null)
                {
                    return false;
                }

                page.Remove(slot);
                CharacterActionMessageHandler.Default.SendDeleteItem(character, page.Identity.Instance, slot);
                return true;
            }
        }

        #endregion
    }
}
