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

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Items;
    using OmniCell.Core.Network;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Loot;
    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// Moving an item from one place to another.
    /// </summary>
    /// <remarks>
    /// The client asks with this and the server answers with it, naming where
    /// the item came from and where it went.
    ///
    /// It was outbound only, which meant the asking half was ignored. That
    /// matters more than the message count suggests: a capture of someone
    /// mounting a vehicle is four MoveItems and nothing else. Mounting a Yalmaha
    /// is moving item 117322 from inventory slot 69 into weapon slot 1, and
    /// dismounting is moving it back. A vehicle is a thing you wield, not a pet
    /// and not a nano, so the whole feature is this message working.
    ///
    /// The work itself is ContainerAddItem's - this says the same thing in fewer
    /// fields, and one implementation of moving an item between two places is
    /// enough.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class MoveItemMessageHandler :
        BaseMessageHandler<MoveItemMessage, MoveItemMessageHandler>
    {
        #region Inbound

        /// <summary>
        /// The client is asking to move an item.
        /// </summary>
        protected override void Read(MoveItemMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            if (message.Source.Type == IdentityType.Backpack
                && (message.Source.Instance >> 16) == CorpseLootAccess.CapturedVirtualSlot)
            {
                IItem taken;
                int destination;
                if (message.Destination == 0x6F
                    && CorpseLootAccess.TryTake(
                        client.Controller.Character,
                        message.Source,
                        out destination,
                        out taken))
                {
                    this.Send(client.Controller.Character, message.Source, destination);
                    QuestManager.OnCollect(
                        client.Controller.Character,
                        TradeSkill.Instance.GetItemName(taken.LowID, taken.HighID, taken.Quality));
                    client.Controller.Character.CalculateSkills();
                }

                return;
            }

            ContainerAddItemMessageHandler.Default.Perform(
                new ContainerAddItemMessage
                    {
                        Identity = message.Identity,
                        Unknown = 0,
                        SourceContainer = message.Source,
                        Target = message.Identity,
                        TargetPlacement = message.Destination
                    },
                client);
        }

        #endregion

        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter character, Identity source, int destination)
        {
            this.Send(character, Filler(character, source, destination), false);
        }

        private static MessageDataFiller Filler(ICharacter character, Identity source, int destination)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Source = source;
                message.Destination = destination;
            };
        }

        #endregion
    }
}
