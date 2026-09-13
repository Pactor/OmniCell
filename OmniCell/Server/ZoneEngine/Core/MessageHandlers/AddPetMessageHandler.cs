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
    /// Gives a character a pet.
    /// </summary>
    /// <remarks>
    /// A capture of a metaphysicist summoning three - Mortificant the Eternal,
    /// Yidira and Zhok the Abomination - shows the whole protocol, and it is two
    /// messages: the pet is described with an ordinary SimpleCharFullUpdate, the
    /// way any character is, and then this says whose it is.
    ///
    /// Three summons, three identical pairs, in that order every time.
    ///
    /// Unknown is 1 in all three, where nearly every other N3 message sends 0.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class AddPetMessageHandler : BaseMessageHandler<AddPetMessage, AddPetMessageHandler>
    {
        #region Constants

        /// <summary>
        /// 1 in all three captured samples.
        /// </summary>
        private const byte UnknownConstant = 1;

        #endregion

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
                message.Unknown = UnknownConstant;
                message.PetIdentity = pet;
            };
        }

        #endregion
    }
}
