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

namespace ZoneEngine.Core.PacketHandlers
{
    #region Usings ...

    using System;
    using System.Collections.Generic;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Network;
    using OmniCell.Enums;

    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Packets;
    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// </summary>
    public static class TradeSkillReceiver
    {
        #region Static Fields

        /// <summary>
        /// </summary>
        private static readonly List<TradeSkillInfo> TradeSkillInfos = new List<TradeSkillInfo>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="sourceItem">
        /// </param>
        /// <param name="targetItem">
        /// </param>
        /// <param name="newItem">
        /// </param>
        /// <returns>
        /// </returns>
        public static string SuccessMessage(Item sourceItem, Item targetItem, Item newItem)
        {
            return string.Format(
                "You combined \"{0}\" with \"{1}\" and the result is a quality level {2} \"{3}\".",
                TradeSkill.Instance.GetItemName(sourceItem.LowID, sourceItem.HighID, sourceItem.Quality),
                TradeSkill.Instance.GetItemName(targetItem.LowID, targetItem.HighID, targetItem.Quality),
                newItem.Quality,
                TradeSkill.Instance.GetItemName(newItem.LowID, newItem.HighID, newItem.Quality));
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="quality">
        /// </param>
        public static void TradeSkillBuildPressed(IZoneClient client, int quality)
        {
            if ((client == null) || (client.Controller == null) || (client.Controller.Character == null))
            {
                return;
            }

            TradeSkillInfo source = client.Controller.Character.TradeSkillSource;
            TradeSkillInfo target = client.Controller.Character.TradeSkillTarget;
            Item sourceItem = SelectedItem(client.Controller.Character, source);
            Item targetItem = SelectedItem(client.Controller.Character, target);
            if ((sourceItem == null) || (targetItem == null)
                || ((source.Container == target.Container) && (source.Placement == target.Placement)))
            {
                return;
            }

            TradeSkillEntry ts = TradeSkill.Instance.GetTradeSkillEntry(sourceItem.HighID, targetItem.HighID);

            if (ts != null)
            {
                // The quality the client asks for is a choice inside the range it was offered, not a
                // free one: what comes out is the target's own quality, plus whatever the builder's
                // skill bumps it by. Taken as it came, a QL 10 implant handed to the Implant
                // Disassembly Clinic asked for a QL 200 Basic Implant back.
                quality = ResultQuality(
                    ts,
                    targetItem.Quality,
                    Bump(ts, targetItem.Quality, client.Controller.Character),
                    quality);
                if (WindowBuild(client, quality, ts, sourceItem, targetItem))
                {
                    Item newItem = new Item(quality, ts.ResultLowId, ts.ResultHighId);
                    InventoryError inventoryError = client.Controller.Character.BaseInventory.TryAdd(newItem);
                    if (inventoryError == InventoryError.OK)
                    {
                        AddTemplateMessageHandler.Default.Send(client.Controller.Character, newItem);

                        string resultName = TradeSkill.Instance.GetItemName(
                            ts.ResultLowId,
                            ts.ResultHighId,
                            quality);
                        QuestManager.OnTradeSkill(client.Controller.Character, resultName, ts.ResultLowId, ts.ResultHighId);

                        // A generic Collect objective intentionally accepts an
                        // item from any route, including a completed build.
                        QuestManager.OnCollect(client.Controller.Character, resultName);

                        // Delete source?
                        if ((ts.DeleteFlag & 1) == 1)
                        {
                            client.Controller.Character.BaseInventory.RemoveItem(source.Container, source.Placement);
                            CharacterActionMessageHandler.Default.SendDeleteItem(
                                client.Controller.Character,
                                source.Container,
                                source.Placement);
                        }

                        // Delete target?
                        if ((ts.DeleteFlag & 2) == 2)
                        {
                            client.Controller.Character.BaseInventory.RemoveItem(target.Container, target.Placement);
                            CharacterActionMessageHandler.Default.SendDeleteItem(
                                client.Controller.Character,
                                target.Container,
                                target.Placement);
                        }

                        ChatTextMessageHandler.Default.Send(
                            client.Controller.Character,
                            SuccessMessage(sourceItem, targetItem, new Item(quality, ts.ResultLowId, ts.ResultHighId)));

                        client.Controller.Character.Stats[StatIds.xp].Value += CalculateXP(quality, ts);
                    }
                }
            }
            else
            {
                ChatTextMessageHandler.Default.Send(
                    client.Controller.Character,
                    "It is not possible to assemble those two items. Maybe the order was wrong?");
                ChatTextMessageHandler.Default.Send(client.Controller.Character, "No combination found!");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="container">
        /// </param>
        /// <param name="placement">
        /// </param>
        public static void TradeSkillSourceChanged(IZoneClient client, int container, int placement)
        {
            if ((client == null) || (client.Controller == null) || (client.Controller.Character == null))
            {
                return;
            }

            if ((container != 0) && (placement != 0))
            {
                var selection = new TradeSkillInfo(0, container, placement);
                Item item = SelectedItem(client.Controller.Character, selection);
                if (item == null)
                {
                    client.Controller.Character.TradeSkillSource = null;
                    return;
                }

                client.Controller.Character.TradeSkillSource = selection;
                TradeSkillPacket.SendSource(
                    client.Controller.Character,
                    TradeSkill.Instance.SourceProcessesCount(item.HighID));

                TradeSkillChanged(client);
            }
            else
            {
                client.Controller.Character.TradeSkillSource = null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="container">
        /// </param>
        /// <param name="placement">
        /// </param>
        public static void TradeSkillTargetChanged(IZoneClient client, int container, int placement)
        {
            if ((client == null) || (client.Controller == null) || (client.Controller.Character == null))
            {
                return;
            }

            if ((container != 0) && (placement != 0))
            {
                var selection = new TradeSkillInfo(0, container, placement);
                Item item = SelectedItem(client.Controller.Character, selection);
                if (item == null)
                {
                    client.Controller.Character.TradeSkillTarget = null;
                    return;
                }

                client.Controller.Character.TradeSkillTarget = selection;
                TradeSkillPacket.SendTarget(
                    client.Controller.Character,
                    TradeSkill.Instance.TargetProcessesCount(item.HighID));

                TradeSkillChanged(client);
            }
            else
            {
                client.Controller.Character.TradeSkillTarget = null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="quality">
        /// </param>
        /// <param name="ts">
        /// </param>
        /// <returns>
        /// </returns>
        private static int CalculateXP(int quality, TradeSkillEntry ts)
        {
            int absMinQL = ItemLoader.ItemList[ts.ResultLowId].Quality;
            int absMaxQL = ItemLoader.ItemList[ts.ResultHighId].Quality;
            if (absMaxQL == absMinQL)
            {
                return ts.MaxXP;
            }

            // Both divisions are done in floating point. As two ints, (MaxXP - MinXP) / (maxQL -
            // minQL) was 0 for every recipe whose quality range is wider than its experience range -
            // 5 to 1000 experience over QL 1 to 200 is 4, but 995/199 as ints is 4 only by luck;
            // 5 to 1000 over 1 to 300 was 3, and anything narrower paid MinXP flat.
            return (int)Math.Floor((ts.MaxXP - ts.MinXP) / (double)(absMaxQL - absMinQL) * (quality - absMinQL) + ts.MinXP);
        }

        /// <summary>
        /// How many quality levels above the target's own the builder's skill is worth.
        /// </summary>
        /// <remarks>
        /// Every skill the recipe asks for is allowed its own bump and the lowest of them wins: a
        /// build is held back by the skill the builder is worst at. This kept whichever skill came
        /// last in the list instead, so a second skill far above its requirement could raise a
        /// result the first skill could not have made. Skills below their requirement bump by
        /// nothing rather than by a negative number.
        ///
        /// Implants have their own ceiling by quality - retail allows one more level per tier - and
        /// a recipe's own MaxBump caps every kind, implants included; it was only ever read for
        /// recipes that are not implants, where nothing read it at all.
        /// </remarks>
        public static int Bump(TradeSkillEntry ts, int targetQuality, ICharacter character)
        {
            return character == null
                       ? 0
                       : Bump(ts, targetQuality, statId => character.Stats[statId].Value);
        }

        /// <summary>
        /// The bump, against any source of skill values. Taking the lookup rather than the character
        /// keeps the rule testable on its own.
        /// </summary>
        public static int Bump(TradeSkillEntry ts, int targetQuality, Func<int, int> skillValue)
        {
            int ceiling = MaxBump(ts, targetQuality);
            if ((ceiling <= 0) || (skillValue == null))
            {
                return 0;
            }

            int bump = ceiling;
            foreach (TradeSkillSkill skill in ts.Skills)
            {
                if (skill.SkillPerBump <= 0)
                {
                    continue;
                }

                int above = skillValue(skill.StatId) - (int)Math.Ceiling(skill.Percent / 100M * targetQuality);
                bump = Math.Min(bump, Math.Max(0, above / skill.SkillPerBump));
            }

            return Math.Max(0, Math.Min(bump, ceiling));
        }

        /// <summary>The most quality levels this recipe may add to its target's own.</summary>
        public static int MaxBump(TradeSkillEntry ts, int targetQuality)
        {
            int ceiling = ts.MaxBump;
            if (ts.IsImplant)
            {
                int tier = targetQuality >= 250 ? 5
                           : targetQuality >= 201 ? 4
                           : targetQuality >= 150 ? 3
                           : targetQuality >= 100 ? 2
                           : targetQuality >= 50 ? 1 : 0;
                ceiling = ceiling > 0 ? Math.Min(ceiling, tier) : tier;
            }

            return Math.Max(0, ceiling);
        }

        /// <summary>
        /// What quality comes out: the target's own, raised by the builder's skill, kept inside the
        /// result template's own range, and never above what the client asked for.
        /// </summary>
        public static int ResultQuality(TradeSkillEntry ts, int targetQuality, int bump, int requested)
        {
            int lowest = ItemLoader.ItemList[ts.ResultLowId].Quality;
            int highest = ItemLoader.ItemList[ts.ResultHighId].Quality;
            int best = Math.Min(targetQuality + Math.Max(0, bump), highest);
            int chosen = requested > 0 ? Math.Min(requested, best) : best;
            return Math.Max(Math.Min(chosen, highest), lowest);
        }

        private static Item SelectedItem(ICharacter character, TradeSkillInfo selection)
        {
            if ((character == null) || (selection == null)
                || !character.BaseInventory.Pages.ContainsKey(selection.Container))
            {
                return null;
            }

            IInventoryPage page = character.BaseInventory.Pages[selection.Container];
            if (!page.ValidSlot(selection.Placement))
            {
                return null;
            }

            return page[selection.Placement] as Item;
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        private static void TradeSkillChanged(IZoneClient client)
        {
            TradeSkillInfo source = client.Controller.Character.TradeSkillSource;
            TradeSkillInfo target = client.Controller.Character.TradeSkillTarget;

            if ((source != null) && (target != null))
            {
                Item sourceItem = SelectedItem(client.Controller.Character, source);
                Item targetItem = SelectedItem(client.Controller.Character, target);
                if ((sourceItem == null) || (targetItem == null))
                {
                    return;
                }

                TradeSkillEntry ts = TradeSkill.Instance.GetTradeSkillEntry(sourceItem.HighID, targetItem.HighID);
                if (ts != null)
                {
                    if (ts.ValidateRange(sourceItem.Quality, targetItem.Quality))
                    {
                        foreach (TradeSkillSkill tsi in ts.Skills)
                        {
                            int skillReq = (int)Math.Ceiling(tsi.Percent / 100M * targetItem.Quality);
                            if (skillReq > client.Controller.Character.Stats[tsi.StatId].Value)
                            {
                                TradeSkillPacket.SendRequirement(client.Controller.Character, tsi.StatId, skillReq);
                            }
                        }

                        int leastbump = Bump(ts, targetItem.Quality, client.Controller.Character);

                        // The same numbers the build will use, so what is offered is what is built.
                        TradeSkillPacket.SendResult(
                            client.Controller.Character,
                            ResultQuality(ts, targetItem.Quality, 0, targetItem.Quality),
                            ResultQuality(ts, targetItem.Quality, leastbump, targetItem.Quality + leastbump),
                            ts.ResultLowId,
                            ts.ResultHighId);
                    }
                    else
                    {
                        TradeSkillPacket.SendOutOfRange(
                            client.Controller.Character,
                            Convert.ToInt32(
                                Math.Round((double)targetItem.Quality - ts.QLRangePercent * targetItem.Quality / 100)));
                    }
                }
                else
                {
                    TradeSkillPacket.SendNotTradeskill(client.Controller.Character);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="desiredQuality">
        /// </param>
        /// <param name="ts">
        /// </param>
        /// <param name="sourceItem">
        /// </param>
        /// <param name="targetItem">
        /// </param>
        /// <returns>
        /// </returns>
        private static bool WindowBuild(
            IZoneClient client,
            int desiredQuality,
            TradeSkillEntry ts,
            Item sourceItem,
            Item targetItem)
        {
            // MinTarget is the lowest quality the target may be, "for things like tier armor where
            // the item must be at max QL to tradeskill. 0 = Any ql" (tradeskill.sql). The test was
            // the other way round, so a recipe with a minimum accepted anything below it and
            // refused everything at or above it.
            if ((ts.MinTargetQL != 0) && (targetItem.Quality < ts.MinTargetQL))
            {
                return false;
            }

            if (!ts.ValidateRange(sourceItem.Quality, targetItem.Quality))
            {
                return false;
            }

            foreach (TradeSkillSkill tss in ts.Skills)
            {
                if (client.Controller.Character.Stats[tss.StatId].Value
                    < Convert.ToInt32(tss.Percent / 100M * targetItem.Quality))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
