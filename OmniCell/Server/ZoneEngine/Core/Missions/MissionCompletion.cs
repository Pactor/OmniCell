#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Missions
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Missions;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// Doing what a mission asked, and being paid for it.
    /// </summary>
    /// <remarks>
    /// What completes a mission is captured per type - see Missions.md - and
    /// three of the five are wired up here: find item is a look, kill person
    /// is a death, and return item is the thing carried back to the terminal.
    /// Find person and repair are not, and say so rather than completing on
    /// something they should not.
    ///
    /// The paying out is one sequence whatever finished it, and it is the one
    /// the 2026-09-26 capture shows:
    ///
    ///   the reward line, the new credit balance, TemplateAction 87 into the
    ///   overflow window, ContainerAddItem out of it, FeedbackMessage
    ///   108871108, CharacterAction MissionChanged, QuestMessage removing the
    ///   mission, and then the key and the objective destroyed.
    /// </remarks>
    public static class MissionCompletion
    {
        /// <summary>
        /// The feedback the live server sends when a mission pays out.
        /// </summary>
        private const int RewardCategory = 110;

        /// <summary>
        /// The feedback the live server sends when a mission pays out.
        /// </summary>
        private const int RewardMessage = 108871108;

        /// <summary>
        /// Somebody looked at something. For a find item mission, looking at
        /// the objective is the whole of it - the item is not picked up and
        /// stays on the floor.
        /// </summary>
        public static void OnLookAt(ICharacter character, Identity target)
        {
            MissionOffer mission = Owning(character, target);
            if (mission != null && mission.Type == MissionType.FindItem)
            {
                Finish(character, mission);
                return;
            }

            // Finding a person is targeting them - there is nothing to pick
            // up and nothing to kill, and the assignment asks only that they
            // be tracked down and observed. The one meant is the monster the
            // building named after the objective, the same one a kill person
            // mission wants dead.
            if (character == null || target.Instance == 0)
            {
                return;
            }

            foreach (MissionOffer found in MissionBook.Active(character))
            {
                if (found.Type != MissionType.FindPerson || found.Built == null)
                {
                    continue;
                }

                if (found.Built.Monsters.Any(
                    m => m.IsObjective && m.Instance == target.Instance))
                {
                    Finish(character, found);
                    return;
                }
            }
        }

        /// <summary>
        /// Somebody picked the objective up. A return item mission is not done
        /// until it is carried back, so this only hands the item over.
        /// </summary>
        /// <returns>Whether the thing taken was a mission objective.</returns>
        public static bool OnTake(ICharacter character, Identity target)
        {
            MissionOffer mission = Owning(character, target);
            if (mission == null)
            {
                return false;
            }

            if (mission.Type != MissionType.ReturnItem)
            {
                // A find item objective is looked at, not taken.
                OnLookAt(character, target);
                return true;
            }

            if (mission.Carried != 0)
            {
                return true;
            }

            try
            {
                IInventoryPage page = character.BaseInventory[character.BaseInventory.StandardPage];
                int slot = page.FindFreeSlot();
                if (slot < 0)
                {
                    Tell(character, "Make room in your inventory for it first.");
                    return true;
                }

                // The objective is an item of its own, at the mission's
                // quality - the captured one reads ACGItemLevel 33 on a QL 33
                // mission, which is the mission's rather than the item's.
                var item = new Item(mission.Quality, ObjectiveTemplate, ObjectiveTemplate);
                if (page.Add(slot, item) != InventoryError.OK)
                {
                    Tell(character, "It would not go into your inventory.");
                    return true;
                }

                mission.Carried = item.Identity.Instance;
                TemplateActionMessageHandler.Default.SendToOverflow(character, item);
                ContainerAddItemMessageHandler.Default.SendFromOverflow(character);
                DespawnMessageHandler.Default.Send(character, target);
                Tell(character, "You pick up the " + (mission.Objective ?? "objective") + ".");
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
            }

            return true;
        }

        /// <summary>
        /// The objective used on a mission terminal, which is how a return
        /// item mission is handed in.
        /// </summary>
        /// <returns>Whether this was a hand-in.</returns>
        public static bool OnHandIn(ICharacter character, Identity terminal)
        {
            if (terminal.Type != IdentityType.MissionTerminal)
            {
                return false;
            }

            MissionOffer mission = MissionBook.Active(character)
                .FirstOrDefault(m => m.Type == MissionType.ReturnItem && m.Carried != 0);
            if (mission == null)
            {
                return false;
            }

            Finish(character, mission);
            return true;
        }

        /// <summary>
        /// The Mission Key Duplicator, used on a mission key.
        /// </summary>
        /// <remarks>
        /// Captured on 2026-09-26: the duplicator is template 28564 and the
        /// key 28577, both quality 1, and using one on the other answers with
        /// a second key of the same template at a new instance. The
        /// duplicator is not consumed - it is still in its slot two zones
        /// later - and the copy is an ordinary item, so it trades, and the
        /// character who receives it walks into the building the original
        /// opens.
        /// </remarks>
        /// <returns>Whether this was a duplication.</returns>
        public static bool OnDuplicate(ICharacter character, Identity first, Identity second)
        {
            if (character == null)
            {
                return false;
            }

            IItem tool = Held(character, first);
            IItem key = Held(character, second);

            // Either way round: the client sends the slots in the order they
            // were clicked and nothing says which is which.
            if (tool != null && key != null && tool.LowID == MissionKeys.Template
                && key.LowID == MissionKeys.DuplicatorTemplate)
            {
                IItem swap = tool;
                tool = key;
                key = swap;
            }

            if (tool == null || key == null
                || tool.LowID != MissionKeys.DuplicatorTemplate
                || key.LowID != MissionKeys.Template)
            {
                return false;
            }

            MissionOffer opens = MissionPlayfields.OpenedBy(new[] { key.Identity.Instance });
            if (opens == null)
            {
                Tell(character, "That key opens nothing any more.");
                return true;
            }

            try
            {
                IInventoryPage page = character.BaseInventory[character.BaseInventory.StandardPage];
                int slot = page.FindFreeSlot();
                if (slot < 0)
                {
                    Tell(character, "Make room in your inventory for the copy.");
                    return true;
                }

                var copy = new Item(MissionKeys.Quality, MissionKeys.Template, MissionKeys.Template);
                if (page.Add(slot, copy) != InventoryError.OK)
                {
                    Tell(character, "The key could not be copied.");
                    return true;
                }

                MissionPlayfields.Cut(copy.Identity.Instance, opens);
                TemplateActionMessageHandler.Default.SendToOverflow(character, copy);
                ContainerAddItemMessageHandler.Default.SendFromOverflow(character);
                Tell(character, "A copy of the " + MissionKeys.Name(opens) + ".");
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
            }

            return true;
        }

        /// <summary>
        /// The item in one of a character's own inventory slots, or null.
        /// </summary>
        private static IItem Held(ICharacter character, Identity slot)
        {
            if (slot.Type != IdentityType.Inventory || character.BaseInventory == null)
            {
                return null;
            }

            foreach (KeyValuePair<int, IInventoryPage> page in character.BaseInventory.Pages)
            {
                foreach (KeyValuePair<int, IItem> held in page.Value.List())
                {
                    if (held.Key == slot.Instance) return held.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Something died. A kill person mission wants the one it named.
        /// </summary>
        public static void OnKill(ICharacter killer, ICharacter victim)
        {
            if (killer == null || victim == null)
            {
                return;
            }

            foreach (MissionOffer mission in MissionBook.Active(killer))
            {
                if (mission.Type != MissionType.KillPerson || mission.Built == null) continue;

                // By instance rather than by name: a building can hold several
                // of one creature and only one of them is the mission's.
                bool wanted = mission.Built.Monsters.Any(
                    m => m.IsObjective && m.Instance == victim.Identity.Instance);
                if (!wanted) continue;

                Finish(killer, mission);
                return;
            }
        }

        /// <summary>
        /// The mission whose objective this is, for a character who is on it.
        /// </summary>
        /// <summary>
        /// Something in the inventory was used on the thing that needs fixing.
        /// </summary>
        /// <remarks>
        /// A repair mission puts a fixture in the building and gives the part
        /// that goes on it - "For the repair task, use this component." - and
        /// the client says so with a GenericCmd UseItemOnItem carrying two
        /// targets, the inventory item first and the fixture second. That is
        /// the same shape a return item hand-in uses, so this is tried after
        /// it and only answers for the fixture of a repair.
        ///
        /// What is **not** checked is which inventory item was used, because
        /// nothing recorded says which one a repair mission hands out. When
        /// that is known this should refuse anything else.
        /// </remarks>
        /// <returns>Whether the thing used on was a repair objective.</returns>
        public static bool OnRepair(ICharacter character, Identity fixture)
        {
            MissionOffer mission = Owning(character, fixture);
            if (mission == null || mission.Type != MissionType.Repair)
            {
                return false;
            }

            Finish(character, mission);
            return true;
        }

        private static MissionOffer Owning(ICharacter character, Identity target)
        {
            if (character == null || target.Instance == 0)
            {
                return null;
            }

            return MissionBook.Active(character).FirstOrDefault(
                m => m.Built != null && m.Objective != null
                     && m.Built.Objective != null
                     && m.Built.Objective.Instance == target.Instance);
        }

        /// <summary>
        /// Pay for it and take it off the list.
        /// </summary>
        public static void Finish(ICharacter character, MissionOffer mission)
        {
            if (character == null || mission == null)
            {
                return;
            }

            try
            {
                character.Stats[StatIds.cash].Value += mission.CashReward;
                character.Stats[StatIds.xp].Value += mission.ExperienceReward;

                if (mission.RewardLowId != 0)
                {
                    IInventoryPage page = character.BaseInventory[character.BaseInventory.StandardPage];
                    int slot = page.FindFreeSlot();
                    if (slot >= 0)
                    {
                        var reward = new Item(
                            mission.Quality, mission.RewardLowId, mission.RewardHighId);
                        if (page.Add(slot, reward) == InventoryError.OK)
                        {
                            TemplateActionMessageHandler.Default.SendToOverflow(character, reward);
                            ContainerAddItemMessageHandler.Default.SendFromOverflow(character);
                        }
                    }
                }

                FeedbackMessageHandler.Default.Send(character, RewardCategory, RewardMessage);

                // Finishing destroys the key and whatever was carried back,
                // which is what the capture shows: CharacterAction 47 on each
                // and then a despawn.
                Destroy(character, mission.KeyInstance);
                Destroy(character, mission.Carried);

                CharacterActionMessageHandler.Default.SendMissionChanged(character, mission.Instance);
                QuestMessageHandler.Default.Send(character, mission.Instance);

                MissionBook.Drop(character, mission.Instance);
                MissionPlayfields.Close(mission);

                Tell(
                    character,
                    "Mission complete. " + mission.CashReward + " credits and "
                    + mission.ExperienceReward + " experience.");
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
            }
        }

        /// <summary>
        /// The template of a mission's objective item.
        /// </summary>
        /// <remarks>
        /// 85639 on the captured return item mission, which is the item the
        /// assignment names - a pair of boots in that case. What decides it is
        /// the item the text was written about, and the text's item is picked
        /// out of the item names table, so the two do not agree yet. Until
        /// they do, the captured one stands in for all of them.
        /// </remarks>
        private const int ObjectiveTemplate = 85639;

        private static void Destroy(ICharacter character, int instance)
        {
            if (instance == 0)
            {
                return;
            }

            foreach (KeyValuePair<int, IInventoryPage> page in character.BaseInventory.Pages)
            {
                foreach (KeyValuePair<int, IItem> slot in page.Value.List().ToList())
                {
                    if (slot.Value == null || slot.Value.Identity.Instance != instance) continue;

                    page.Value.Remove(slot.Key);
                    CharacterActionMessageHandler.Default.SendDeleteItem(
                        character, (int)slot.Value.Identity.Type, instance);
                    return;
                }
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
    }
}
