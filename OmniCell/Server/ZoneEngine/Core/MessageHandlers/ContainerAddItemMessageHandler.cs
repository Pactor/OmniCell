#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    // TODO: Change Actions to something more suitable (maybe EntityAction?)
    using System;
    using System.Linq;
    using System.Threading;

    using OmniCell.Core.Actions;
    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Network;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Packets;
    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.All)]
    public class ContainerAddItemMessageHandler :
        BaseMessageHandler<ContainerAddItemMessage, ContainerAddItemMessageHandler>
    {
        #region Static Fields

        /// <summary>
        /// The item functions whose effect is on the wire in AppearanceUpdate.
        /// </summary>
        private static readonly int[] AppearanceFunctions =
            {
                (int)FunctionType.HeadMesh, (int)FunctionType.BackMesh, (int)FunctionType.Shouldermesh,
                (int)FunctionType.Texture, (int)FunctionType.ChangeBodyMesh, (int)FunctionType.AttractorMesh
            };

        #endregion

        #region Inbound

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <param name="client">
        /// </param>
        /// <summary>
        /// Performs a move that arrived as some other message.
        /// </summary>
        /// <remarks>
        /// MoveItem says the same thing in fewer fields, and there is no reason
        /// for two implementations of moving an item between two places.
        /// </remarks>
        public void Perform(ContainerAddItemMessage message, IZoneClient client)
        {
            this.Read(message, client);
        }

        protected override void Read(ContainerAddItemMessage message, IZoneClient client)
        {
            bool noAppearanceUpdate = false;

            /* Container ID's:
             * 0065 Weaponpage
             * 0066 Armorpage
             * 0067 Implantpage
             * 0068 Inventory (places 64-93)
             * 0069 Bank
             * 006B Backpack
             * 006C KnuBot Trade Window
             * 006E Overflow window
             * 006F Trade Window
             * 0073 Socialpage
             * 0767 Shop Inventory
             * 0790 Playershop Inventory
             * DEAD Trade Window (incoming) It's bank now (when you put something into the bank)
             */

            IInventoryPage sendingPage = Pool.Instance.GetObject<IInventoryPage>(
                message.Identity,
                new Identity()
                {
                    Type = (IdentityType)message.Identity.Instance,
                    Instance = (int)message.SourceContainer.Type
                });
            int fromPlacement = message.SourceContainer.Instance;
            Identity toIdentity = message.Target;
            int toPlacement = message.TargetPlacement;

            // Where and what does need to be moved/added?
            IItem itemFrom = sendingPage[fromPlacement];

            // Receiver of the item (IInstancedEntity, can be mostly all from NPC, Character or Bag, later even playfields)
            // Turn 0xDEAD into C350 if instance is the same
            if (toIdentity.Type == IdentityType.IncomingTradeWindow)
            {
                toIdentity.Type = IdentityType.CanbeAffected;
            }

            IItemContainer itemReceiver =
                client.Controller.Character.Playfield.FindByIdentity(toIdentity) as IItemContainer;
            if (itemReceiver == null)
            {
                throw new ArgumentOutOfRangeException(
                    "No Entity found: " + message.Target.Type.ToString() + ":" + message.Target.Instance);
            }

            // On which inventorypage should the item be added?
            IInventoryPage receivingPage;
            if ((toPlacement == 0x6f) && (message.Target.Type == IdentityType.IncomingTradeWindow))
            {
                receivingPage = itemReceiver.BaseInventory.Pages[(int)IdentityType.Bank];
            }
            else
            {
                receivingPage = itemReceiver.BaseInventory.PageFromSlot(toPlacement);
            }

            // Get standard page if toplacement cant be found (0x6F for next free slot)
            // TODO: If Entities are not the same (other player, bag etc) then always add to the standard page
            if ((receivingPage == null) || (itemReceiver.GetType() != client.Controller.Character.GetType()))
            {
                receivingPage = itemReceiver.BaseInventory.Pages[itemReceiver.BaseInventory.StandardPage];
            }

            if (receivingPage == null)
            {
                throw new ArgumentOutOfRangeException("No inventorypage found.");
            }

            if (toPlacement == 0x6f)
            {
                toPlacement = receivingPage.FindFreeSlot();
            }

            // Is there already a item?
            IItem itemTo;
            try
            {
                itemTo = receivingPage[toPlacement];
            }
            catch (Exception)
            {
                itemTo = null;
            }

            // Calculating delay for equip/unequip/switch gear
            int delay = 20;

            client.Controller.Character.DoNotDoTimers = true;
            IItemSlotHandler equipTo = receivingPage as IItemSlotHandler;
            IItemSlotHandler unequipFrom = sendingPage as IItemSlotHandler;

            noAppearanceUpdate =
                !((equipTo is WeaponInventoryPage) || (equipTo is ArmorInventoryPage)
                  || (equipTo is SocialArmorInventoryPage));
            noAppearanceUpdate &=
                !((unequipFrom is WeaponInventoryPage) || (unequipFrom is ArmorInventoryPage)
                  || (unequipFrom is SocialArmorInventoryPage));

            // What actually moved on or off an equipment page, for deciding
            // whether the appearance has to be sent.
            bool moved = false;
            IItem displaced = null;

            if (equipTo != null)
            {
                if (itemTo != null)
                {
                    if (receivingPage.NeedsItemCheck)
                    {
                        // The requirements are those of the page the item goes on. This asked the
                        // page it came from - the inventory, which has none - so swapping into an
                        // occupied equipment slot skipped every requirement.
                        AOAction action = this.getAction(receivingPage, itemFrom);

                        if (FitsSlot(receivingPage, itemFrom, toPlacement)
                            && action.CheckRequirements(client.Controller.Character))
                        {
                            UnEquip.Send(client, receivingPage, toPlacement);
                            if (!noAppearanceUpdate)
                            {
                                // Equipmentpages need delays
                                // Delay when equipping/unequipping
                                // has to be redone, jumping breaks the equiping/unequiping 
                                // and other messages have to be done too
                                // like heartbeat timer, damage from environment and such

                                delay = (itemFrom.GetAttribute(211) == 1234567890 ? 20 : itemFrom.GetAttribute(211))
                                        + (itemTo.GetAttribute(211) == 1234567890 ? 20 : itemTo.GetAttribute(211));
                            }

                            // social has to wait for 0.2 secs too (for helmet update). The wait holds this
                            // client's message queue, so an item with a huge or broken equip delay is capped
                            // at three seconds rather than stalling the client.
                            Thread.Sleep(Math.Min(Math.Max(delay, 0), 300) * 10);

                            client.Controller.Character.Send(message);

                            // Only a hand carries a weapon instance. The NCU belt
                            // in slot 7 is equipped with ContainerAddItem and
                            // TemplateAction alone in both captures of it
                            // (newchar_s20 seq 11343, 20260913-070754 seq 651).
                            if ((equipTo is WeaponInventoryPage)
                                && Combat.CombatWeaponProfiles.IsPlayerWeaponSlot(toPlacement))
                            {
                                WeaponItemFullUpdateMessageHandler.Default.SendForEquip(
                                    client.Controller.Character,
                                    itemFrom,
                                    toPlacement);
                            }
                            equipTo.HotSwap(sendingPage, fromPlacement, toPlacement);
                            displaced = itemTo;
                            moved = true;
                            Equip.Send(client, receivingPage, toPlacement);

                            // The last of Dr. Mason's quests asks for the
                            // implant to be worn, not merely carried.
                            QuestManager.OnEquip(
                                client.Controller.Character,
                                TradeSkill.Instance.GetItemName(
                                    itemFrom.LowID,
                                    itemFrom.HighID,
                                    itemFrom.Quality),
                                itemFrom.LowID,
                                itemFrom.HighID);
                        }
                    }
                }
                else
                {
                    if (receivingPage.NeedsItemCheck)
                    {
                        if (itemFrom == null)
                        {
                            throw new NullReferenceException("itemFrom can not be null, possible inventory error");
                        }

                        AOAction action = this.getAction(receivingPage, itemFrom);

                        if (FitsSlot(receivingPage, itemFrom, toPlacement)
                            && action.CheckRequirements(client.Controller.Character))
                        {
                            if (!noAppearanceUpdate)
                            {
                                // Equipmentpages need delays
                                // Delay when equipping/unequipping
                                // has to be redone, jumping breaks the equiping/unequiping 
                                // and other messages have to be done too
                                // like heartbeat timer, damage from environment and such

                                delay = itemFrom.GetAttribute(211);
                                if ((equipTo is SocialArmorInventoryPage) || (delay == 1234567890))
                                {
                                    delay = 20;
                                }

                                Thread.Sleep(Math.Min(Math.Max(delay, 0), 300) * 10); // capped, as above
                            }

                            if (sendingPage == receivingPage)
                            {
                                // Switch rings for example
                                UnEquip.Send(client, sendingPage, fromPlacement);
                            }

                            client.Controller.Character.Send(message);

                            // Only a hand carries a weapon instance. The NCU belt
                            // in slot 7 is equipped with ContainerAddItem and
                            // TemplateAction alone in both captures of it
                            // (newchar_s20 seq 11343, 20260913-070754 seq 651).
                            if ((equipTo is WeaponInventoryPage)
                                && Combat.CombatWeaponProfiles.IsPlayerWeaponSlot(toPlacement))
                            {
                                WeaponItemFullUpdateMessageHandler.Default.SendForEquip(
                                    client.Controller.Character,
                                    itemFrom,
                                    toPlacement);
                            }
                            equipTo.Equip(sendingPage, fromPlacement, toPlacement);
                            moved = true;
                            Equip.Send(client, receivingPage, toPlacement);

                            QuestManager.OnEquip(
                                client.Controller.Character,
                                TradeSkill.Instance.GetItemName(
                                    itemFrom.LowID,
                                    itemFrom.HighID,
                                    itemFrom.Quality),
                                itemFrom.LowID,
                                itemFrom.HighID);
                        }
                    }
                }
            }
            else
            {
                if (unequipFrom != null)
                {
                    // Send to client first
                    if (!noAppearanceUpdate)
                    {
                        // Equipmentpages need delays
                        // Delay when equipping/unequipping
                        // has to be redone, jumping breaks the equiping/unequiping 
                        // and other messages have to be done too
                        // like heartbeat timer, damage from environment and such

                        delay = itemFrom.GetAttribute(211);
                        if ((unequipFrom is SocialArmorInventoryPage) || (delay == 1234567890))
                        {
                            delay = 20;
                        }

                        Thread.Sleep(Math.Min(Math.Max(delay, 0), 300) * 10); // capped, as above
                    }

                    UnEquip.Send(client, sendingPage, fromPlacement);
                    unequipFrom.Unequip(fromPlacement, receivingPage, toPlacement);
                    moved = true;

                    // The client asks for 0x6F, "anywhere", and the live server
                    // answers with the slot the item went to: taking eight pieces
                    // of armor off in a row came back as placements 78 to 85.
                    // Echoing 0x6F told the client nothing about where it was.
                    message.TargetPlacement = toPlacement;
                    client.Controller.Character.Send(message);
                }
                else
                {
                    // No equipment page involved, just send ContainerAddItemMessage back
                    message.TargetPlacement = receivingPage.FindFreeSlot();
                    IItem item = sendingPage.Remove(fromPlacement);
                    receivingPage.Add(message.TargetPlacement, item);
                    client.Controller.Character.Send(message);
                }
            }

            client.Controller.Character.DoNotDoTimers = false;

            // Moving an existing item between the player's own pages is not a
            // collection event. Taking one from a world container is.
            if (itemFrom != null
                && itemReceiver == client.Controller.Character
                && message.Identity != client.Controller.Character.Identity
                && message.SourceContainer.Type != IdentityType.KnuBotTradeWindow
                && message.SourceContainer.Type != IdentityType.IncomingTradeWindow)
            {
                QuestManager.OnCollect(
                    client.Controller.Character,
                    TradeSkill.Instance.GetItemName(itemFrom.LowID, itemFrom.HighID, itemFrom.Quality));
            }

            // Apply item functions before sending the appearanceupdate message
            client.Controller.Character.CalculateSkills();

            // The live server sends the appearance after putting on or taking off
            // anything that changes it, and after nothing else. In a capture of
            // eight armor pieces coming off and going back on, every one was
            // followed by an AppearanceUpdate, including the ones that left the
            // picture unchanged; the two NCU chips, the NCU belt and the weapon
            // on the same run had none, and neither does an implant or a neck
            // item in any capture. The difference is whether the item has a
            // function that draws something.
            if (moved && (ChangesAppearance(itemFrom) || ChangesAppearance(displaced)))
            {
                AppearanceUpdateMessageHandler.Default.Send(client.Controller.Character);
            }
        }

        /// <summary>
        /// Whether the item may go in that slot of the page. Only implant slots are checked so far:
        /// an implant or spirit fits the slots its Placement names, so a leg implant no longer goes
        /// in the eye. A refused move is answered like a failed requirement, with nothing.
        /// </summary>
        private static bool FitsSlot(IInventoryPage page, IItem item, int slot)
        {
            ImplantInventoryPage implants = page as ImplantInventoryPage;
            return (implants == null) || implants.Fits(item, slot);
        }

        /// <summary>
        /// Whether wearing an item draws anything.
        /// </summary>
        private static bool ChangesAppearance(IItem item)
        {
            return item != null
                   && item.Events.Any(
                       x => x.EventType == EventType.OnWear
                            && x.Functions.Any(f => AppearanceFunctions.Contains(f.FunctionType)));
        }

        /// <summary>
        /// </summary>
        /// <param name="page">
        /// </param>
        /// <param name="item">
        /// </param>
        /// <returns>
        /// </returns>
        private AOAction getAction(IInventoryPage page, IItem item)
        {
            AOAction action = null;

            // TODO: Add special check for social page
            if ((page is ArmorInventoryPage) || (page is ImplantInventoryPage))
            {
                action = item.ItemActions.SingleOrDefault(x => x.ActionType == ActionType.ToWear);
                if (action == null)
                {
                    return new AOAction();
                }
            }

            if (page is WeaponInventoryPage)
            {
                action = item.ItemActions.SingleOrDefault(x => x.ActionType == ActionType.ToWield);
                if (action == null)
                {
                    return new AOAction();
                }
            }

            if (page is PlayerInventoryPage)
            {
                // No checks needed for unequipping
                return new AOAction();
            }

            if (page is SocialArmorInventoryPage)
            {
                // TODO: Check for side, sex, breed conditionals
                return new AOAction();
            }

            if (action == null)
            {
                throw new NotSupportedException(
                    "No suitable action found for equipping to this page: " + page.GetType());
            }

            return action;
        }

        public void Send(ICharacter character, Identity sourceContainer, int slot)
        {
            this.Send(character, this.FillContainerAddItem(character, sourceContainer, slot));
        }

        /// <summary>
        /// The move that follows an overflow TemplateAction: from OverflowWindow:0
        /// to the player's own overflow window, at 0x6F, which is how every copy
        /// reads (20260911-171203_s12 seq 1591-1632, 20260909-141545 seq 730 and
        /// 2849-2855).
        /// </summary>
        public void SendFromOverflow(ICharacter character)
        {
            this.Send(
                character,
                x =>
                {
                    x.Identity = character.Identity;
                    x.Unknown = 0;
                    x.SourceContainer = new Identity { Type = IdentityType.OverflowWindow, Instance = 0 };
                    x.Target = new Identity { Type = IdentityType.OverflowWindow, Instance = character.Identity.Instance };
                    x.TargetPlacement = 0x6F;
                });
        }

        private MessageDataFiller FillContainerAddItem(ICharacter character, Identity sourceContainer, int slot)
        {
            return x =>
            {
                x.Identity = character.Identity;
                x.SourceContainer = sourceContainer;
                x.TargetPlacement = slot;
                x.Target = character.Identity;
                x.Unknown = 0;
            };
        }

        #endregion
    }
}
