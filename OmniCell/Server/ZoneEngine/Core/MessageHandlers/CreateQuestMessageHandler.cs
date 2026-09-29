#region License

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
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Network;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using Utility;

    using OmniCell.Core.Missions;

    using ZoneEngine.Core.Missions;

    #endregion

    /// <summary>
    /// A mission taken off a terminal's list.
    /// </summary>
    /// <remarks>
    /// The client sends the identity of the offer it picked. What the server
    /// sends back, in this order and as captured on 2026-09-26:
    ///
    ///   1. the key, which is an item of template 28577 named for the building;
    ///   2. `QuestFullUpdate` with AnnounceAsNew set, carrying the mission
    ///      under a **new** identity - the five offers were ...3380 to ...3384,
    ///      the one accepted was ...3383 and the quest granted was ...3385.
    ///
    /// The new identity is not derived from the offer's, so it is allocated
    /// here rather than reused.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.InboundOnly)]
    public class CreateQuestMessageHandler :
        BaseMessageHandler<CreateQuestMessage, CreateQuestMessageHandler>
    {
        #region Constants

        /// <summary>
        /// The mission key's template, from itemnames: "Mission Key to".
        /// </summary>
        /// <remarks>
        /// 28577 low and high, quality 1 whatever the mission's own quality is
        /// - the captured key reads ACGItemLevel 1 on a QL 33 mission.
        /// </remarks>
        private const int KeyTemplate = 28577;

        /// <summary>The quality every captured mission key carries.</summary>
        private const int KeyQuality = 1;

        #endregion

        public CreateQuestMessageHandler()
        {
            this.UpdateCharacterStatsOnReceive = false;
        }

        #region Inbound

        protected override void Read(CreateQuestMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            ICharacter character = client.Controller.Character;
            MissionOffer offer = MissionBook.OfferedTo(character, message.QuestIdentity.Instance);
            if (offer == null)
            {
                // Either an authored quest, which this server starts through
                // its own conversation paths, or a stale mission window.
                return;
            }

            if (MissionBook.Active(character).Count >= MissionBook.MostAtOnce)
            {
                Tell(character, "You are already on as many missions as you can carry.");
                return;
            }

            // The accepted mission is a different quest from the offer.
            offer.Instance = Pool.Instance.GetFreeInstance<CreateQuestMessage>(1, IdentityType.Quest);

            // The building is made now rather than when the door is used, so
            // that a mission that cannot be built is refused here - where the
            // player is standing at the terminal and can take another one -
            // rather than at a door that does nothing.
            offer.Built = MissionBuilding.Build(
                offer,
                () => Pool.Instance.GetFreeInstance<CreateQuestMessage>(1, IdentityType.Door));
            if (offer.Built == null)
            {
                Tell(character, "That mission could not be prepared. Try another.");
                return;
            }

            offer.BuildingInstance = Pool.Instance.GetFreeInstance<CreateQuestMessage>(
                1, MissionBuilding.BuildingType);
            offer.PlayfieldInstance = Pool.Instance.GetFreeInstance<CreateQuestMessage>(
                1, IdentityType.Playfield2);

            if (!this.GiveKey(character, offer))
            {
                return;
            }

            if (!this.GivePart(character, offer))
            {
                return;
            }

            MissionBook.Take(character, offer);
            MissionPlayfields.Open(offer);
            MissionPlayfields.Cut(offer.KeyInstance, offer);

            QuestFullUpdateMessageHandler.Default.SendMissions(
                character, MissionBook.Active(character), true);
        }

        /// <summary>
        /// The key into the character's inventory.
        /// </summary>
        /// <remarks>
        /// Retail puts it there with a SimpleItemFullUpdate naming an inventory
        /// slot. This server has one working way of handing an item over - the
        /// overflow window, which is how an authored quest's accept-time items
        /// arrive - so the key goes that way and the player sees it land. The
        /// difference is cosmetic and is written down rather than hidden.
        /// </remarks>
        /// <summary>
        /// The part a repair mission is done with.
        /// </summary>
        /// <remarks>
        /// "For the repair task, use this component." Only a repair mission
        /// has one; everything else returns true having done nothing.
        ///
        /// A mission whose part could not be handed over is still taken - the
        /// building is made and the key is given - because refusing it here
        /// would leave the player holding a key to a mission that is no
        /// longer theirs. They are told instead.
        /// </remarks>
        private bool GivePart(ICharacter character, MissionOffer offer)
        {
            if (offer.Type != MissionType.Repair || offer.ComponentLowId == 0)
            {
                return true;
            }

            try
            {
                IInventoryPage page = character.BaseInventory[character.BaseInventory.StandardPage];
                int slot = page.FindFreeSlot();
                if (slot < 0)
                {
                    Tell(character, "No room for the repair part - make space and take another.");
                    return true;
                }

                var part = new Item(
                    Math.Max(1, offer.Quality), offer.ComponentLowId, offer.ComponentLowId);
                if (page.Add(slot, part) != InventoryError.OK)
                {
                    Tell(character, "The repair part could not be handed over.");
                    return true;
                }

                offer.ComponentInstance = part.Identity.Instance;
                TemplateActionMessageHandler.Default.SendToOverflow(character, part);
                ContainerAddItemMessageHandler.Default.SendFromOverflow(character);
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
            }

            return true;
        }

        private bool GiveKey(ICharacter character, MissionOffer offer)
        {
            try
            {
                IInventoryPage page = character.BaseInventory[character.BaseInventory.StandardPage];
                if (page.FindFreeSlot() < 0)
                {
                    Tell(character, "Make room in your inventory for the mission key.");
                    return false;
                }

                var key = new Item(KeyQuality, KeyTemplate, KeyTemplate);
                int slot = page.FindFreeSlot();
                if (page.Add(slot, key) != InventoryError.OK)
                {
                    Tell(character, "The mission key could not be handed over.");
                    return false;
                }

                offer.KeyInstance = key.Identity.Instance;
                TemplateActionMessageHandler.Default.SendToOverflow(character, key);
                ContainerAddItemMessageHandler.Default.SendFromOverflow(character);
                return true;
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
                Tell(character, "The mission key could not be handed over.");
                return false;
            }
        }

        private static void Tell(ICharacter character, string text)
        {
            if (character == null || character.Playfield == null)
            {
                return;
            }

            try
            {
                character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
            }
        }

        #endregion
    }
}
