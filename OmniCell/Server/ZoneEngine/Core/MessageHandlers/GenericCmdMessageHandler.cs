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

    // TODO: Make this to EntityEnvent or something like this
    using System;
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Events;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Network;
    using OmniCell.Enums;
    using OmniCell.Interfaces;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Loot;
    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.All)]
    public class GenericCmdMessageHandler : BaseMessageHandler<GenericCmdMessage, GenericCmdMessageHandler>
    {
        #region Inbound

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <param name="client">
        /// </param>
        /// <exception cref="NullReferenceException">
        /// </exception>
        protected override void Read(GenericCmdMessage message, IZoneClient client)
        {
            switch (message.Action)
            {
                case GenericCmdAction.Get:
                    break;
                case GenericCmdAction.Drop:
                    break;
                case GenericCmdAction.Use:
                    if (message.Target[0].Type == IdentityType.Inventory)
                    {
                        client.Controller.UseItem(message.Target[0]);

                        // Acknowledge action
                        this.Acknowledge(client.Controller.Character, message);
                    }
                    else
                    {
                        if (Pool.Instance.Contains(message.Target[0]))
                        {
                            // A corpse is a normal item container. Opening it
                            // sends its current entries; taking one uses the
                            // same ContainerAddItem path as bags and inventory.
                            if (message.Target[0].Type == IdentityType.Corpse)
                            {
                                CorpseLoot corpse = Pool.Instance.GetObject<CorpseLoot>(
                                    client.Controller.Character.Playfield.Identity,
                                    message.Target[0]);
                                if (corpse != null
                                    && CorpseLootAccess.IsOpen(client.Controller.Character, corpse.Identity))
                                {
                                    // The second use closes it. A corpse closed with nothing left in it
                                    // is taken away (see Playfield.CorpseEmptied).
                                    CorpseLootAccess.ForgetCharacter(client.Controller.Character.Identity);
                                    this.Acknowledge(client.Controller.Character, message);
                                    var playfield = client.Controller.Character.Playfield as OmniCell.Core.Playfields.Playfield;
                                    if (playfield != null && CorpseLootAccess.IsEmpty(corpse))
                                    {
                                        playfield.CorpseEmptied(corpse.Identity);
                                    }
                                }
                                else if (corpse != null)
                                {
                                    int virtualSlot = CorpseLootAccess.Open(client.Controller.Character, corpse);
                                    InventoryUpdateMessageHandler.Default.SendForCorpse(
                                        client.Controller.Character,
                                        corpse,
                                        virtualSlot);
                                    this.Acknowledge(client.Controller.Character, message);
                                }

                                break;
                            }

                            // TODO: Call OnUse of the targets controller
                            // Static dynels first
                            IEventHolder temp = null;
                            try
                            {
                                temp =
                                    Pool.Instance.GetObject<IEventHolder>(
                                        client.Controller.Character.Playfield.Identity,
                                        message.Target[0]);
                            }
                            catch (Exception)
                            {
                            }
                            if (temp != null)
                            {
                                var fixture = temp as StaticDynel;
                                if (fixture != null)
                                {
                                    // Gone until it comes back: nothing to use.
                                    if (fixture.Hidden)
                                    {
                                        break;
                                    }

                                    // Using a fixture is a quest event whether or not its template has
                                    // an OnUse event. The quest Cargo Box has none, and this path never
                                    // told the quests - opening it never advanced "Open the Cargo Box".
                                    FixtureBehaviours.Used(client.Controller.Character, fixture);
                                    QuestManager.OnUse(client.Controller.Character, message.Target[0], fixture.Template.ID);
                                }

                                var entity = temp as IEntity;
                                if (entity != null)
                                {
                                    Event ev = temp.Events.FirstOrDefault(x => x.EventType == EventType.OnUse);
                                    if (ev != null)
                                    {
                                        ev.Perform(client.Controller.Character, entity);
                                        this.Acknowledge(client.Controller.Character, message);
                                    }
                                    else
                                    {
                                        ev = temp.Events.FirstOrDefault(x => x.EventType == EventType.OnTrade);
                                        if (ev != null)
                                        {
                                            ev.Perform(client.Controller.Character, entity);

                                            TemporaryBag tempBag = new TemporaryBag(
                                                client.Controller.Character.Identity,
                                                new Identity()
                                                {
                                                    Type = IdentityType.TempBag,
                                                    Instance =
                                                        Pool.Instance.GetFreeInstance<TemporaryBag>(
                                                            0,
                                                            IdentityType.TempBag)
                                                },
                                                client.Controller.Character.Identity,
                                                message.Target[0]);
                                            client.Controller.Character.ShoppingBag = tempBag;
                                            TradeMessageHandler.Default.Send(client.Controller.Character, tempBag);
                                            this.Acknowledge(client.Controller.Character, message);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Use statel (doors, grid terminals etc)
#if DEBUG
                            string s = string.Format(
                                "Generic Command received:\r\nAction: {0} ({1}){2}Target: {3} {4}",
                                message.Action,
                                (int)message.Action,
                                Environment.NewLine,
                                message.Target[0].Type,
                                message.Target[0].ToString(true));
                            ChatTextMessageHandler.Default.Send(client.Controller.Character, s);
#endif
                            // A fixture the client has from its own playfield data and the server never
                            // spawns: the Merchant's Strongbox, where the thief hides (20260911-163012_s8
                            // #2372, #6729). Using it is a quest event all the same.
                            QuestManager.OnUse(client.Controller.Character, message.Target[0]);

                            // The Surgery Clinic runs its own data and opens the implant window;
                            // retail acknowledges the use (20260909-142713 s3 7195).
                            if (SurgeryClinic.TryUse(client.Controller.Character, message.Target[0]))
                            {
                                this.Acknowledge(client.Controller.Character, message);
                                break;
                            }

                            client.Controller.UseStatel(message.Target[0]);
                        }
                    }

                    break;
                case GenericCmdAction.UseItemOnItem:
                    IItem item =
                        Pool.Instance.GetObject<IInventoryPage>(
                            new Identity()
                            {
                                Type = (IdentityType)client.Controller.Character.Identity.Instance,
                                Instance = (int)message.Target[0].Type
                            })[message.Target[0].Instance];
                    client.Controller.Character.Stats[StatIds.secondaryitemtemplate].Value = item.LowID;

                    // Fixture templates test the item used on them against stat 83: the Gas Fire's
                    // OnUseItemOn requires "Self 83 EqualTo 296780", the Compact Fire Suppressant
                    // Container (items.ocp). With only 273 set the requirement never held.
                    client.Controller.Character.Stats[StatIds.secondaryiteminstance].Value = item.LowID;
                    //client.Controller.Character.Stats[StatIds.secondaryitemtype]
                    bool pooledTarget = Pool.Instance.Contains(message.Target[1]);
                    NLog.LogManager.GetCurrentClassLogger().Info(
                        "USEON character={0} item={1}:{2} low={3} target={4}:{5} pooled={6}",
                        client.Controller.Character.Identity.Instance,
                        message.Target[0].Type,
                        message.Target[0].Instance,
                        item == null ? 0 : item.LowID,
                        message.Target[1].Type,
                        message.Target[1].Instance,
                        pooledTarget);
                    if (pooledTarget)
                    {
                        StaticDynel temp =
                            Pool.Instance.GetObject<StaticDynel>(
                                client.Controller.Character.Playfield.Identity,
                                message.Target[1]);
                        if (temp != null)
                        {
                            if (temp.Hidden)
                            {
                                break;
                            }

                            Event ev = temp.Events.FirstOrDefault(x => x.EventType == EventType.OnUseItemOn);
                            // Only the event's requirements are asked (the Gas Fire: the item used is the
                            // Compact Fire Suppressant Container). Its functions are not run: this server
                            // applies template functions to the player, and the fire's "Set timeexist 0" is
                            // meant for the fire. Going out, the feedback line and the quest come from
                            // FixtureBehaviours and QuestManager below.
                            bool allowed = ev == null
                                           || ev.Functions.Any(
                                               f => OmniCell.Core.Requirements.Requirement.CheckAll(
                                                   f.Requirements,
                                                   client.Controller.Character));
                            NLog.LogManager.GetCurrentClassLogger().Info(
                                "USEON fixture template={0} hidden={1} event={2} allowed={3}",
                                temp.Template.ID,
                                temp.Hidden,
                                ev != null,
                                allowed);
                            if (allowed)
                            {
                                // The fixture's feedback first ("You extinguish the Gas Fire."), then the
                                // quest, as retail orders them (20260914-124401 #2122-2125).
                                FixtureBehaviours.Used(client.Controller.Character, temp);
                                QuestManager.OnUseItemOn(
                                    client.Controller.Character,
                                    message.Target[1],
                                    TradeSkill.Instance.GetItemName(
                                        temp.Template.ID,
                                        temp.Template.ID,
                                        temp.Template.Quality),
                                    temp.Template.ID,
                                    item == null ? 0 : item.LowID,
                                    item == null ? 0 : item.HighID);
                            }
                        }
                        else
                        {
                            QuestManager.OnUseItemOn(client.Controller.Character, message.Target[1]);
                        }
                    }
                    else
                    {
                        QuestManager.OnUseItemOn(client.Controller.Character, message.Target[1]);
                        client.Controller.UseStatel(message.Target[1], EventType.OnUseItemOn);
                    }
                    break;
            }
        }

        #endregion

        #region Outbound

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="message">
        /// </param>
        /// <param name="announceToPlayfield">
        /// </param>
        public void Acknowledge(ICharacter character, GenericCmdMessage message, bool announceToPlayfield = false)
        {
            this.Send(character, this.Reply(character, message), announceToPlayfield);
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="message">
        /// </param>
        /// <returns>
        /// </returns>
        private MessageDataFiller Reply(ICharacter character, GenericCmdMessage message)
        {
            return x =>
            {
                x.Identity = message.Identity;
                x.N3MessageType = message.N3MessageType;
                x.Target = message.Target.ToList().ToArray();
                x.Verification = 1;
                x.Serial = message.Serial;
                x.Action = message.Action;
                x.Flag = message.Flag;
                x.User = message.User;
                x.Unknown = 0;
            };
        }

        #endregion
    }
}
