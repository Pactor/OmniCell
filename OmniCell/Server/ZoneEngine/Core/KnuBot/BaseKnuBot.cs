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

namespace ZoneEngine.Core.KnuBot
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;

    using OmniCell.Enums;

    using ZoneEngine.Core.Controllers;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using OmniCell.ObjectManager;

    using ZoneEngine.Core.Quests;

    using Utility;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// </summary>
    public class BaseKnuBot
    {
        /// <summary>
        /// </summary>
        public WeakRef<ICharacter> Character;

        /// <summary>
        /// </summary>
        public Identity KnuBotIdentity;

        /// <summary>
        /// </summary>
        private KnuBotDialogTree rootNode;

        /// <summary>
        /// </summary>
        private KnuBotDialogTree selectedNode;

        private bool suppressDialogContinuation;

        /// <summary>
        /// </summary>
        /// <param name="knubotIdentity">
        /// </param>
        /// <param name="root">
        /// </param>
        protected BaseKnuBot(Identity knubotIdentity, KnuBotDialogTree root)
            : this(knubotIdentity)
        {
            this.SetRootNode(root);
        }

        /// <summary>
        /// </summary>
        /// <param name="knubotIdentity">
        /// </param>
        protected BaseKnuBot(Identity knubotIdentity)
        {
            this.Character = new WeakRef<ICharacter>(null);
            this.KnuBotIdentity = knubotIdentity;
        }

        /// <summary>
        /// Creates the per-player conversation object used by an NPC. Legacy
        /// code-driven bots return themselves; database-scripted bots override
        /// this and return an isolated session.
        /// </summary>
        public virtual BaseKnuBot CreateSession()
        {
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="node">
        /// </param>
        protected void SetRootNode(KnuBotDialogTree node)
        {
            node.ValidateTree();
            this.rootNode = node;
            this.selectedNode = this.rootNode;
            this.rootNode.SetKnuBot(this);
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        public ICharacter GetCharacter()
        {
            /*            if (this.Character.Target == null)
                        {
                            throw new Exception("Character has gone away.");
                        }
                        */
            return this.Character.Target;
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <returns>
        /// </returns>
        public bool StartDialog(ICharacter character)
        {
            bool result = false;

            // Does the starting character exist?
            if (character != null)
            {
                ICharacter current = this.GetCharacter();
                if (current != null && current.Identity != character.Identity)
                {
                    return false;
                }

                // OK, no one else is talking, lets initialize
                result = true;
                this.Character.Target = character;
                this.selectedNode = this.rootNode;
                this.OpenWindow();

                    // Opening the conversation is what a "talk to" objective
                    // asks for, and it is asked for by twenty eight of Arete
                    // Landing's quests. Before the answer, because a character
                    // who has just finished an errand should be told so while
                    // the window is opening rather than after they close it.
                    QuestManager.OnTalk(
                        character,
                        Pool.Instance.GetObject<ICharacter>(
                            character.Playfield.Identity,
                            this.KnuBotIdentity));

                this.Answer(KnuBotOptionId.DialogStart);
                LogUtil.Debug(DebugInfoDetail.KnuBot, string.Format("KnuBut Start Dialog"));
            }

            return result;
        }

        public bool IsTalkingTo(ICharacter character)
        {
            ICharacter current = this.GetCharacter();
            return current != null && character != null && current.Identity == character.Identity;
        }

        /// <summary>
        /// </summary>
        /// <param name="id">
        /// </param>
        public void Answer(KnuBotOptionId id)
        {
            this.Answer((int)id);
        }

        /// <summary>
        /// </summary>
        /// <param name="answer">
        /// </param>
        /// <exception cref="Exception">
        /// </exception>
        /// <summary>
        /// The option number of the answer being handled.
        /// </summary>
        /// <remarks>
        /// A dialog action is called with no arguments, so an action that has to
        /// know which option led to it has nowhere to read it from. This is that
        /// somewhere. Set before the node executes and meaningful only during
        /// it.
        /// </remarks>
        protected int LastAnswer { get; private set; }

        public void Answer(int answer)
        {
            this.LastAnswer = answer;
            this.suppressDialogContinuation = false;
            KnuBotDialogTree oldNode = this.selectedNode;
            // Only do talk if window is still open
            if (answer != (int)KnuBotOptionId.WindowClosed)
            {
                string nextId = this.selectedNode.Execute((KnuBotOptionId)answer);
                LogUtil.Debug(
                    DebugInfoDetail.KnuBot,
                    string.Format(
                        "Received KnuBot Answer {0} for node {1} -> {2}",
                        answer,
                        this.selectedNode.id,
                        nextId));
                if (nextId == "parent")
                {
                    this.selectedNode = this.selectedNode.Parent;
                }
                else
                {
                    if (nextId == "root")
                    {
                        this.selectedNode = this.rootNode;
                    }
                    else
                    {
                        if (nextId != "self")
                        {
                            KnuBotDialogTree nextSelectedNode = this.selectedNode.GetNode(nextId);
                            if (nextSelectedNode == null)
                            {
                                throw new Exception(
                                    "Could not find dialog id '" + nextId + "' in tree '"
                                    + string.Join(Environment.NewLine, this.selectedNode.FlattenDialogIds()) + "'");
                            }

                            this.selectedNode = nextSelectedNode;
                        }
                    }
                }

                // Only start over if its not the same node or option
                if (this.Character.Target != null && !this.suppressDialogContinuation)
                {
                    if ((answer != (int)KnuBotOptionId.DialogStart) || (oldNode != this.selectedNode))
                    {
                        this.Answer(KnuBotOptionId.DialogStart);
                    }
                }
            }
            else
            {
                // Anything in the trade window goes back before the player it
                // belongs to is let go of. After that line there is nobody to
                // give it to and the items are simply gone.
                this.ReturnEverything();

                // Remove link to conversation partner
                this.Character = new WeakRef<ICharacter>(null);
            }
        }

        /// <summary>
        /// Keeps Answer from immediately entering the next dialogue step. Used
        /// while a trade window owns the interaction.
        /// </summary>
        protected void SuspendDialogContinuation()
        {
            this.suppressDialogContinuation = true;
        }

        /// <summary>
        /// </summary>
        /// <param name="action">
        /// </param>
        /// <param name="nextId">
        /// </param>
        /// <returns>
        /// </returns>
        protected KnuBotActionStruct CAS(KnuBotAction action, string nextId)
        {
            return new KnuBotActionStruct() { ActionId = action.Method.Name, BotAction = action, NextDialogId = nextId };
        }

        /// <summary>
        /// </summary>
        /// <param name="choices">
        /// </param>
        protected void SendAnswerList(params string[] choices)
        {
            KnuBotAnswerListMessageHandler.Default.Send(this.GetCharacter(), this.KnuBotIdentity, choices);
            LogUtil.Debug(
                DebugInfoDetail.KnuBot,
                string.Format("Sending KnuBot Choice List ({0} choices)", choices.Length));
        }

        /// <summary>
        /// </summary>
        /// <param name="text">
        /// </param>
        protected void Write(string text)
        {
            KnuBotAppendTextMessageHandler.Default.Send(this.GetCharacter(), this.KnuBotIdentity, text);
            LogUtil.Debug(DebugInfoDetail.KnuBot, string.Format("KnuBut Write"));
            // Need to sleep here, else packets will be jumbled...
            Thread.Sleep(20);
        }

        protected void WriteLine(string text = "")
        {
            this.Write(text + "\n");
        }

        /// <summary>
        /// A line with the client's emote flag: 1 for a narrated line ("Rex lowers his voice.").
        /// </summary>
        protected void WriteLine(string text, int flag)
        {
            KnuBotAppendTextMessageHandler.Default.Send(this.GetCharacter(), this.KnuBotIdentity, text + "\n", flag);
            Thread.Sleep(20);
        }

        /// <summary>
        /// </summary>
        protected void OpenWindow()
        {
            KnuBotOpenChatWindowMessageHandler.Default.Send(this.GetCharacter(), this.KnuBotIdentity);
            LogUtil.Debug(DebugInfoDetail.KnuBot, "Opening KnuBot window");
        }

        /// <summary>
        /// </summary>
        /// <param name="items">
        /// </param>
        protected void RejectItems(IEnumerable<Item> items)
        {
            KnuBotRejectedItemsMessageHandler.Default.Send(this.GetCharacter(), this.KnuBotIdentity, items);
            LogUtil.Debug(DebugInfoDetail.KnuBot, string.Format("KnuBut Reject {0} items", items.Count()));
        }

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <param name="numberOfSlots">
        /// </param>
        protected void StartTrade(string message, int numberOfSlots = 6)
        {
            KnuBotStartTradeMessageHandler.Default.Send(
                this.GetCharacter(),
                this.KnuBotIdentity,
                message,
                numberOfSlots);
            LogUtil.Debug(DebugInfoDetail.KnuBot, string.Format("KnuBut Start trade ({0} slots)", numberOfSlots));
        }

        /// <summary>
        /// </summary>
        /// <param name="item">
        /// </param>
        /// <summary>
        /// The conversation a character is having, if it is having one.
        /// </summary>
        public static BaseKnuBot Of(ICharacter npc, ICharacter talker = null)
        {
            NPCController controller = npc == null ? null : npc.Controller as NPCController;
            return controller == null
                       ? null
                       : talker == null ? controller.KnuBot : controller.KnuBotFor(talker);
        }

        /// <summary>
        /// What the player has dragged into the trade window and not yet handed
        /// over.
        /// </summary>
        /// <remarks>
        /// Held here rather than left in the inventory because the client has
        /// already moved it: as far as the player can see the item is in the
        /// window, and an inventory that still lists it would let the same item
        /// be spent twice. Held rather than destroyed because the trade is not
        /// finished - it can be taken back out, declined, or refused by whoever
        /// is being handed it, and every one of those has to give it back.
        ///
        /// What stood here took the item out of the inventory the moment it
        /// went into the window and never put it anywhere. Dragging something
        /// in and changing your mind destroyed it.
        /// </remarks>
        private readonly List<Traded> escrow = new List<Traded>();

        /// <summary>
        /// Whatever is in the trade window at the moment.
        /// </summary>
        protected List<Traded> Escrow
        {
            get
            {
                return this.escrow;
            }
        }

        /// <summary>
        /// The player put an item into the trade window.
        /// </summary>
        public virtual void TradeAdd(Identity where)
        {
            ICharacter talker = this.GetCharacter();
            if (talker == null)
            {
                return;
            }

            Item item = talker.BaseInventory.GetItemInContainer((int)where.Type, where.Instance);
            if (item == null)
            {
                return;
            }

            IInventoryPage sourcePage;
            if (!talker.BaseInventory.Pages.TryGetValue((int)where.Type, out sourcePage)
                || !sourcePage.List().Remove(where.Instance))
            {
                return;
            }

            // Escrow is an in-memory reservation. Do not delete the persisted
            // item here: a rejected, cancelled, disconnected or failed trade
            // must leave the durable inventory unchanged. A successful quest
            // hand-in persists the consumed/remainder inventory together with
            // quest state and rewards in QuestManager's transaction.
            this.escrow.Add(new Traded { Where = where, Item = item });

            LogUtil.Debug(
                DebugInfoDetail.KnuBot,
                string.Format("KnuBot trade: item from container {0} slot {1}", where.Type, where.Instance));
        }

        /// <summary>
        /// The player took an item back out of the trade window.
        /// </summary>
        public virtual void TradeRemove(Identity where)
        {
            Traded held =
                this.escrow.FirstOrDefault(t => t.Where.Type == where.Type && t.Where.Instance == where.Instance);

            // The window numbers its own slots, and no capture says whether the
            // client quotes those or the inventory slot the item came from back
            // at us. With one thing in there it makes no difference which, and
            // handing back the wrong item would be worse than being careful.
            if (held == null && this.escrow.Count == 1)
            {
                held = this.escrow[0];
            }

            if (held == null)
            {
                return;
            }

            this.escrow.Remove(held);
            this.GiveBack(held, true);
        }

        /// <summary>
        /// The player closed the trade window.
        /// </summary>
        /// <remarks>
        /// Gives everything back. A character that wants any of it overrides
        /// this, keeps what it asked for, and calls here for the rest.
        /// </remarks>
        public virtual void TradeFinish(bool declined)
        {
            this.ReturnEverything();
        }

        /// <summary>
        /// Empties the trade window back into the player's inventory.
        /// </summary>
        protected void ReturnEverything()
        {
            foreach (Traded held in this.escrow.ToList())
            {
                this.GiveBack(held, true);
            }

            this.escrow.Clear();
        }

        /// <summary>
        /// Ends a conversation whose client connection has already gone away.
        /// Escrow is restored before the character is persisted, but no packet
        /// is written to the dead socket.
        /// </summary>
        public void Disconnect()
        {
            foreach (Traded held in this.escrow.ToList())
            {
                this.GiveBack(held, false);
            }

            this.escrow.Clear();
            this.Character = new WeakRef<ICharacter>(null);
        }

        protected void GiveBack(Traded held, bool notifyClient)
        {
            ICharacter talker = this.GetCharacter();
            if (talker == null)
            {
                LogUtil.Debug(DebugInfoDetail.KnuBot, "KnuBot trade: nobody to give the item back to");
                return;
            }

            IInventoryPage originalPage;
            bool returned = false;
            try
            {
                returned = talker.BaseInventory.Pages.TryGetValue((int)held.Where.Type, out originalPage)
                           && originalPage.ValidSlot(held.Where.Instance)
                           && originalPage[held.Where.Instance] == null
                           && originalPage.Add(held.Where.Instance, held.Item) == InventoryError.OK;
            }
            catch (ArgumentException)
            {
                // The original slot changed after the availability check.
                // Fall back to the normal inventory allocator below.
                returned = false;
            }

            if (!returned && talker.BaseInventory.TryAdd(held.Item) != InventoryError.OK)
            {
                LogUtil.Debug(DebugInfoDetail.KnuBot, "KnuBot trade: no room to give the item back");
                return;
            }

            if (notifyClient)
            {
                // Out of the window and into the first free slot of the main
                // inventory. 0x6f is what the vendor path uses to say that.
                ContainerAddItemMessageHandler.Default.Send(
                    talker,
                    new Identity { Type = IdentityType.KnuBotTradeWindow, Instance = held.Where.Instance },
                    0x6f);
            }
        }

        /// <summary>
        /// An item sitting in the trade window, and where it came from.
        /// </summary>
        protected sealed class Traded
        {
            public Identity Where;

            public Item Item;
        }

        public void CloseChatWindow()
        {
            this.CloseChatWindow(3);
        }

        public void CloseChatWindow(int seconds)
        {
            // Before the talker is let go of, for the same reason as in Answer.
            this.ReturnEverything();

            ICharacter talker = this.Character.Target;
            KnuBotCloseChatWindowMessageHandler.Default.Send(talker, this.KnuBotIdentity, seconds);
            this.Character = new WeakRef<ICharacter>(null);

            // The server closed it, and the client does not say so when the window goes on its own timer.
            // A session left behind makes the character refuse every later conversation with the player.
            if (talker != null && talker.Playfield != null)
            {
                ICharacter npc = Pool.Instance.GetObject<ICharacter>(talker.Playfield.Identity, this.KnuBotIdentity);
                NPCController controller = npc == null ? null : npc.Controller as NPCController;
                if (controller != null)
                {
                    controller.EndKnuBotDialog(talker, this);
                }
            }
            LogUtil.Debug(DebugInfoDetail.KnuBot, string.Format("Close KnuBot window"));
        }
    }
}
