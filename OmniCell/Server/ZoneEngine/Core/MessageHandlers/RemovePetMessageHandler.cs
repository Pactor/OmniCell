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

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// Takes a pet away from its master.
    /// </summary>
    /// <remarks>
    /// The mirror of AddPet, and the same shape: master identity, pet identity.
    ///
    /// A capture of three pets being terminated shows the pair it comes in -
    /// RemovePet, then Despawn for the same pet, once each, three times over.
    /// The pet stops being anybody's before it stops existing.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class RemovePetMessageHandler : BaseMessageHandler<RemovePetMessage, RemovePetMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter master, Identity pet)
        {
            this.Send(master, Filler(master, pet), true);
        }

        private static MessageDataFiller Filler(ICharacter master, Identity pet)
        {
            return message =>
            {
                message.Identity = master.Identity;
                message.Unknown = 0;
                message.PetIdentity = pet;
            };
        }

        #endregion
    }
}
