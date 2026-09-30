#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Combat
{
    #region Usings ...

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Cell.Core;

    using MsgPack;

    using OmniCell.Core.Actions;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Events;
    using OmniCell.Core.Functions;

    using ZoneEngine.Core.Functions;
    using OmniCell.Core.Items;
    using OmniCell.Core.Requirements;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using Utility;

    #endregion

    /// <summary>
    /// Pressing a perk, and the wait before it can be pressed again.
    /// </summary>
    /// <remarks>
    /// Measured from one retail session - a Keeper in a QL250 mission, 48
    /// minutes, 142 perk presses. The walkthrough with the raw bytes is
    /// <c>AOBuddy10/docs/evidence-perks-and-looting.md</c> and the summary is
    /// <c>Documentation/Perks-And-Looting.md</c>. Nothing here is invented;
    /// where the recording did not show something it is left undone and said
    /// so.
    ///
    /// The whole of a perk is authored in its own item, which is why almost
    /// nothing is hard-coded here. Seppuku Slash, item 226382, reads:
    ///
    ///     stats:   AttackDelay = 50
    ///     ToUse:   Flags HasPerk 778 ; EquippedWeapons BitAnd 2
    ///     event 0: CastNano(209980) on the target
    ///              LockPerk(AttackSpeed, 778, 95)
    ///              SpecialHit(Health, -5345, -11877) if target below 15% health
    ///              ... three more brackets ...
    ///              SystemText("You successfully perform a Seppuku Slash attack.")
    ///
    /// Every number the capture measured is in there: the queue delay is
    /// <c>AttackDelay</c>, the cooldown is the third LockPerk argument, and
    /// the success line is the SystemText. So this runs the item's own script
    /// rather than five special cases, and a perk nobody has tried works the
    /// same way as one that has.
    ///
    /// Three things the server sends that the item does not carry, and which
    /// are therefore here:
    ///
    ///   - <see cref="CharacterActionType.QueuePerk"/> (80) P1=2 P2=delay, the
    ///     moment the press arrives.
    ///   - <see cref="CharacterActionType.PerkUnavailable"/> (207) and
    ///     <see cref="CharacterActionType.PerkAvailable"/> (206), which are how
    ///     availability is told - <b>no stat carries a perk cooldown</b>.
    ///     Nothing but Health, IsFightingMe, SocialStatus, Clan, ClanLevel and
    ///     Cash was sent for the player in the whole recording.
    ///   - the refusal texts in <see cref="Refusal"/>.
    ///
    /// **The perk's target is the last LookAt**, not the message's Target
    /// field, which was the player itself in all 142 presses. That is
    /// <see cref="ICharacter.SelectedTarget"/>, which is what a function
    /// argument of target 3 already resolves to.
    /// </remarks>
    public static class Perks
    {
        /// <summary>
        /// What the client adds to a perk's short id when it presses it.
        /// </summary>
        private const int PressOffset = 10000;

        /// <summary>
        /// A hundredth of a second, which is what the queue delay is in.
        /// </summary>
        private const double Centisecond = 0.01;

        /// <summary>
        /// The texts retail answered a refused press with, all category 110.
        /// </summary>
        private static class Refusal
        {
            /// <summary>"Item must be applied on a friendly target."</summary>
            public const int Unfriendly = 106156137;

            /// <summary>"You are already running this action!"</summary>
            public const int Queued = 171187118;

            /// <summary>"This item requires a fighting-target to be applied on."</summary>
            public const int NeedsTarget = 25614836;
        }

        private const int FeedbackCategory = 110;

        /// <summary>
        /// A press that has not run yet.
        /// </summary>
        private sealed class Queued
        {
            public int Perk;

            public ItemTemplate Item;

            public Identity Target;

            public DateTime Runs;
        }

        /// <summary>
        /// Presses waiting to run, in the order they were made.
        /// </summary>
        /// <remarks>
        /// A list rather than one entry, because the recording shows four
        /// perks pressed inside two seconds each getting their own queue
        /// acknowledgement and running one after another in press order.
        /// </remarks>
        private static readonly ConcurrentDictionary<Identity, List<Queued>> Waiting =
            new ConcurrentDictionary<Identity, List<Queued>>();

        /// <summary>
        /// When each locked perk comes back, by character and short id.
        /// </summary>
        private static readonly ConcurrentDictionary<Identity, Dictionary<int, DateTime>> Locked =
            new ConcurrentDictionary<Identity, Dictionary<int, DateTime>>();

        /// <summary>
        /// What a perk gives you: the action id, the four letter code and the
        /// item whose script runs.
        /// </summary>
        public sealed class PerkAction
        {
            /// <summary>The perk's short id, which is what a character owns.</summary>
            public int Perk;

            /// <summary>The id the client presses with - the short id plus ten thousand.</summary>
            public int Action;

            /// <summary>The four letter code, packed the way the wire carries it.</summary>
            public int Code;

            /// <summary>The item carrying the cast, the damage and the cooldown.</summary>
            public ItemTemplate Item;
        }

        /// <summary>
        /// Every perk action in the pack, by the short id that grants it.
        /// </summary>
        /// <remarks>
        /// From AddAction (53182), which is the client database's own
        /// statement of the matter. A perk line item carries one - Blessing 1,
        /// item 211702, reads <c>AddAction(10190, "LAON", 1, 215791)</c> -
        /// and those four values are exactly what the server sends and what
        /// the client sends back:
        ///
        ///   argument 0  the action id, 10190, which is 10000 + the short id
        ///   argument 1  the code, "LAON", as CharacterAction 180 carries it
        ///   argument 3  the item whose script the press runs
        ///
        /// This replaced looking the item up by its <c>HasPerk</c>
        /// requirement, which was how both halves used to find it and which
        /// was not good enough. Measured against the thirty CharacterAction
        /// 180 messages in the retail recording, the HasPerk route resolved
        /// 17 and silently skipped 13 - every AI perk, whose action item
        /// carries no such requirement - while this reproduces 29 of the 30
        /// exactly, item and code both. The one it misses is Unhallowed Wrath
        /// (20010), which has no AddAction anywhere in the pack and whose
        /// mechanism the recording did not settle either.
        /// </remarks>
        private static Dictionary<int, PerkAction> actions;

        private static readonly object Gate = new object();

        /// <summary>
        /// The perk actions, by the short id each one belongs to.
        /// </summary>
        public static Dictionary<int, PerkAction> Actions
        {
            get
            {
                lock (Gate)
                {
                    return actions ?? (actions = Index());
                }
            }
        }

        private static Dictionary<int, PerkAction> Index()
        {
            var found = new Dictionary<int, PerkAction>();
            foreach (ItemTemplate item in ItemLoader.ItemList.Values)
            {
                if (item.Events == null)
                {
                    continue;
                }

                foreach (Event ev in item.Events)
                {
                    if (ev.Functions == null)
                    {
                        continue;
                    }

                    foreach (Function function in ev.Functions)
                    {
                        if (function.FunctionType != (int)FunctionType.AddAction
                            || function.Arguments == null || function.Arguments.Values == null
                            || function.Arguments.Values.Count < 4)
                        {
                            continue;
                        }

                        var values = function.Arguments.Values;
                        int action = values[0].AsInt32();
                        int perk = action - PressOffset;
                        int carrier = values[3].AsInt32();
                        ItemTemplate runs;
                        if (perk <= 0 || !ItemLoader.ItemList.TryGetValue(carrier, out runs))
                        {
                            continue;
                        }

                        // Every level of a perk line repeats the same
                        // AddAction, so the first is as good as the last.
                        if (found.ContainsKey(perk))
                        {
                            continue;
                        }

                        found[perk] = new PerkAction
                                          {
                                              Perk = perk,
                                              Action = action,
                                              Code = Code(values[1]),
                                              Item = runs,
                                          };
                    }
                }
            }

            LogUtil.Debug(DebugInfoDetail.Engine, "Perks: " + found.Count + " perk actions in the item pack.");
            return found;
        }

        /// <summary>
        /// The four letter code as the wire carries it.
        /// </summary>
        /// <remarks>
        /// The pack holds it as a string - "LAON" - and the messages carry it
        /// as an int with the first letter in the top byte, which is
        /// 0x4C414F4E. Verified both ways round in the recording: the server's
        /// CharacterAction 180 for perk 190 reads 0x4C414F4E and the client's
        /// press of it reads the same.
        ///
        /// Nothing in the pack stores the code as an int, so a non-string
        /// argument is refused rather than converted on a guess about which
        /// way round it would be.
        /// </remarks>
        private static int Code(MessagePackObject value)
        {
            if (!value.IsTypeOf<string>().GetValueOrDefault())
            {
                return 0;
            }

            string text = value.AsString() ?? string.Empty;
            int code = 0;
            for (int i = 0; i < 4 && i < text.Length; i++)
            {
                code |= (text[i] & 0xFF) << ((3 - i) * 8);
            }

            return code;
        }

        /// <summary>
        /// The client pressed a perk.
        /// </summary>
        /// <remarks>
        /// <paramref name="pressed"/> is the message's Parameter1, which is
        /// the short id plus ten thousand for every perk the recording caught
        /// except Unhallowed Wrath (20010), whose mechanism is not determined
        /// - it is answered by neither a queue nor a cooldown, and is left
        /// alone here rather than guessed at.
        /// </remarks>
        public static void Press(ICharacter character, int pressed, int code)
        {
            if (character == null || character.Controller == null || character.Controller.Client == null)
            {
                return;
            }

            int perk = pressed - PressOffset;
            if (perk <= 0 || perk >= PressOffset)
            {
                LogUtil.Debug(
                    DebugInfoDetail.Engine,
                    "Perks: " + character.Identity.Instance + " pressed " + pressed + " (" + Code(code)
                    + "), which is not a perk this server knows how to run.");
                return;
            }

            PerkAction action;
            if (!Actions.TryGetValue(perk, out action))
            {
                LogUtil.Debug(DebugInfoDetail.Engine, "Perks: nothing in the pack grants perk " + perk + ".");
                return;
            }

            ItemTemplate item = action.Item;

            List<Queued> queue = Waiting.GetOrAdd(character.Identity, id => new List<Queued>());
            lock (queue)
            {
                if (queue.Any(q => q.Perk == perk))
                {
                    Say(character, Refusal.Queued);
                    return;
                }
            }

            if (IsLocked(character, perk))
            {
                // The recording never caught a press of a locked perk - the
                // client greys them out - so there is no measured answer to
                // send. Saying nothing is what a server that has not been told
                // should do.
                return;
            }

            Identity target = character.SelectedTarget;
            if (target.Type == IdentityType.None || target.Instance == 0)
            {
                target = character.Identity;
            }

            if (!Allowed(character, item, target))
            {
                return;
            }

            int delay = Delay(item);
            lock (queue)
            {
                queue.Add(
                    new Queued
                    {
                        Perk = perk,
                        Item = item,
                        Target = target,
                        Runs = DateTime.UtcNow.AddSeconds(delay * Centisecond),
                    });
            }

            Send(
                character,
                new CharacterActionMessage
                {
                    Identity = character.Identity,
                    Action = CharacterActionType.QueuePerk,
                    Target = character.Identity,
                    Parameter1 = 2,
                    Parameter2 = delay,
                    Unknown = 0,
                });
        }

        /// <summary>
        /// Whether this perk can be applied to what is being looked at.
        /// </summary>
        /// <remarks>
        /// Two refusals were caught and both are here. A friendly-only perk
        /// aimed at something hostile - Lay On Hands at a mob - and a hostile
        /// perk whose target had died a fifth of a second earlier.
        ///
        /// Which perks are friendly-only is read off the item rather than
        /// listed: an item that only heals has no hostile effect in it, and
        /// one that hits does. The recording agrees - Lay On Hands is all
        /// CastNano, Seppuku Slash is all SpecialHit.
        /// </remarks>
        private static bool Allowed(ICharacter character, ItemTemplate item, Identity target)
        {
            bool hostile = Hits(item);
            IInstancedEntity aimed = character.Playfield.FindByIdentity(target);
            var victim = aimed as ICharacter;

            if (hostile)
            {
                if (victim == null || victim.Stats[StatIds.health].Value == 0)
                {
                    Say(character, Refusal.NeedsTarget);
                    return false;
                }

                return true;
            }

            // A friendly perk on something that is not on your side.
            if (victim != null && victim.Identity != character.Identity
                && victim.Stats[StatIds.side].Value != character.Stats[StatIds.side].Value)
            {
                Say(character, Refusal.Unfriendly);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Whether this perk does damage, which is what makes it hostile.
        /// </summary>
        private static bool Hits(ItemTemplate item)
        {
            if (item.Events == null)
            {
                return false;
            }

            return (from ev in item.Events
                    where ev.Functions != null
                    from fn in ev.Functions
                    select fn.FunctionType).Any(
                f => f == (int)FunctionType.SpecialHit || f == (int)FunctionType.Hit
                     || f == (int)FunctionType.AreaHit || f == (int)FunctionType.DrainHit);
        }

        /// <summary>
        /// How long the client is told to wait before the perk goes off.
        /// </summary>
        /// <remarks>
        /// The item's AttackDelay, in hundredths of a second. It read 50 for
        /// Seppuku Slash, 100 for Lay On Hands, Devotional Armor and Blade
        /// Whirlwind and 200 for Honoring the Ancients, which are exactly the
        /// three values the server sent, and the measured gaps to execution
        /// matched them.
        /// </remarks>
        private static int Delay(ItemTemplate item)
        {
            int delay;
            // AOSharp calls stat 294 AttackDelay; OmniCell's enum calls it
            // itemdelay. It is the same stat, and on a perk action item it is
            // the wait the server tells the client to expect.
            return item.Stats != null && item.Stats.TryGetValue((int)StatIds.itemdelay, out delay) && delay > 0
                       ? delay
                       : 100;
        }

        /// <summary>
        /// Runs whatever is due, once per playfield heartbeat.
        /// </summary>
        public static void Tick(ICharacter character)
        {
            if (character == null)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;

            List<Queued> queue;
            if (Waiting.TryGetValue(character.Identity, out queue))
            {
                Queued due = null;
                lock (queue)
                {
                    // One at a time and in press order, which is the order the
                    // recording ran four of them in.
                    if (queue.Count > 0 && queue[0].Runs <= now)
                    {
                        due = queue[0];
                        queue.RemoveAt(0);
                    }
                }

                if (due != null)
                {
                    Run(character, due);
                }
            }

            Dictionary<int, DateTime> locks;
            if (!Locked.TryGetValue(character.Identity, out locks))
            {
                return;
            }

            List<int> back;
            lock (locks)
            {
                back = locks.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList();
                foreach (int perk in back)
                {
                    locks.Remove(perk);
                }
            }

            foreach (int perk in back)
            {
                Send(
                    character,
                    new CharacterActionMessage
                    {
                        Identity = character.Identity,
                        Action = CharacterActionType.PerkAvailable,
                        Target = character.Identity,
                        Parameter1 = 0,
                        Parameter2 = perk,
                        Unknown = 0,
                    });
            }
        }

        /// <summary>
        /// Runs the perk's own script.
        /// </summary>
        /// <remarks>
        /// Every function on the item's first event, in the order it is
        /// written, against the target the press was aimed at. The functions
        /// do the rest: CastNano puts the "Affected by" nano on, LockPerk
        /// starts the cooldown, SpecialHit does the damage and SystemText says
        /// what happened.
        ///
        /// What is <b>not</b> sent yet is the TemplateAction that closed each
        /// of the recorded sequences - it names the perk item and the target,
        /// and what the client does with it is not known, so it is left out
        /// rather than sent wrongly.
        /// </remarks>
        private static void Run(ICharacter character, Queued due)
        {
            if (due.Item.Events == null)
            {
                return;
            }

            IInstancedEntity target = character.Playfield.FindByIdentity(due.Target) ?? character;

            foreach (Event ev in due.Item.Events)
            {
                if (ev.Functions == null)
                {
                    continue;
                }

                foreach (Function function in ev.Functions)
                {
                    try
                    {
                        if (!Requirement.CheckAll(function.Requirements, character))
                        {
                            continue;
                        }

                        FunctionCollection.Instance.CallFunction(
                            function.FunctionType,
                            character,
                            character,
                            Resolve(function.Target, character, target),
                            function.Arguments.Values.ToArray());
                    }
                    catch (Exception exception)
                    {
                        LogUtil.ErrorException(exception);
                    }
                }

                // Only the first event is the press. The others are other
                // things happening to the item - Seppuku Slash carries a
                // second LockPerk on event 27, and what fires that is not
                // known.
                break;
            }
        }

        /// <summary>
        /// What a function's target number means for a perk.
        /// </summary>
        /// <remarks>
        /// Perk items use two: 3, the selected target, which is the LookAt the
        /// perk was aimed at; and 2, the wearer, which is whoever pressed it.
        /// </remarks>
        private static IInstancedEntity Resolve(int number, ICharacter character, IInstancedEntity target)
        {
            switch (number)
            {
                case 2:
                case 26:
                case 100:
                    return character;
                default:
                    return target;
            }
        }

        /// <summary>
        /// Starts a perk's cooldown and tells the client.
        /// </summary>
        /// <remarks>
        /// Called by the LockPerk function, which is how the item says it.
        /// The five cooldowns in the recording - 40, 120, 110, 45 and 95
        /// seconds - are each the item's own third argument, and each measured
        /// unlock came within a second and a half of it.
        /// </remarks>
        public static void Lock(ICharacter character, int perk, int seconds)
        {
            if (character == null || seconds <= 0)
            {
                return;
            }

            Dictionary<int, DateTime> locks =
                Locked.GetOrAdd(character.Identity, id => new Dictionary<int, DateTime>());
            lock (locks)
            {
                locks[perk] = DateTime.UtcNow.AddSeconds(seconds);
            }

            Send(
                character,
                new CharacterActionMessage
                {
                    Identity = character.Identity,
                    Action = CharacterActionType.PerkUnavailable,
                    Target = character.Identity,
                    Parameter1 = perk,
                    Parameter2 = seconds,
                    Unknown = 0,
                });
        }

        /// <summary>
        /// Whether this perk is still cooling down.
        /// </summary>
        public static bool IsLocked(ICharacter character, int perk)
        {
            Dictionary<int, DateTime> locks;
            if (!Locked.TryGetValue(character.Identity, out locks))
            {
                return false;
            }

            lock (locks)
            {
                DateTime until;
                return locks.TryGetValue(perk, out until) && until > DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Forgets a character's queue and cooldowns.
        /// </summary>
        public static void Forget(ICharacter character)
        {
            List<Queued> queue;
            Waiting.TryRemove(character.Identity, out queue);
            Dictionary<int, DateTime> locks;
            Locked.TryRemove(character.Identity, out locks);
        }

        private static void Say(ICharacter character, int message)
        {
            Send(
                character,
                new FeedbackMessage
                {
                    Identity = character.Identity,
                    Unknown = 0,
                    Unknown1 = 0,
                    CategoryId = FeedbackCategory,
                    MessageId = message,
                });
        }

        /// <summary>
        /// What the client needs to put each of the character's perk actions in
        /// the Perk Actions menu. Without it the menu never appears.
        /// </summary>
        /// <remarks>
        /// Straight after FullCharacter, one CharacterAction 180 per perk
        /// action: Target 0:&lt;action item&gt;, Parameter1 the action id,
        /// Parameter2 the four byte code. For a Keeper with Blessing 1 that is
        /// Target 0:215791 (Lay On Hands), 10190, bytes 4E 4F 41 4C.
        ///
        /// The numbers are the perk item's own AddAction (53182) arguments in
        /// the client database - Blessing 1, item 211702: 10190, the code, 1,
        /// 215791 - so they are read from the item pack rather than listed here.
        /// </remarks>
        public static void AnnounceActions(ICharacter character)
        {
            var owner = character as Character;
            if (owner == null)
            {
                return;
            }

            var sent = new HashSet<int>();
            foreach (int perk in owner.Perks)
            {
                PerkAction action;
                if (!Actions.TryGetValue(perk, out action) || !sent.Add(action.Action))
                {
                    continue;
                }

                Send(
                    character,
                    new CharacterActionMessage
                    {
                        Identity = character.Identity,
                        Action = CharacterActionType.PerkAction,
                        Target = new Identity { Type = IdentityType.None, Instance = action.Item.ID },
                        Parameter1 = action.Action,
                        Parameter2 = action.Code,
                        Unknown = 0,
                    });
            }
        }

        private static void Send(ICharacter character, MessageBody message)
        {
            if (character.Controller != null && character.Controller.Client != null)
            {
                character.Controller.Client.SendCompressed(message);
            }
        }

        /// <summary>
        /// The four letter code the client sends, for the log.
        /// </summary>
        private static string Code(int code)
        {
            var letters = new char[4];
            for (int i = 0; i < 4; i++)
            {
                letters[i] = (char)((code >> ((3 - i) * 8)) & 0xFF);
            }

            return new string(letters);
        }
    }
}
