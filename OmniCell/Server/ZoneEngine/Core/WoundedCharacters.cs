namespace ZoneEngine.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Functions;
    using OmniCell.Core.Items;
    using OmniCell.Core.Playfields;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using Utility;

    using ZoneEngine.Core.MessageHandlers;

    /// <summary>
    /// The wounded lying about Arete Landing, and the Health Regeneration Stim that gets one of them
    /// back on their feet for a while.
    /// </summary>
    /// <remarks>
    /// A Wounded Dockworker is 32 health with 20 missing, sitting on the ground (movement mode 8), in
    /// every retail sighting. The stim used on one, retail (20260914-124401 s4, dockworker 2052536078):
    /// <list type="bullet">
    /// <item>12:47:50.99 the stim is used; the quest step is solved.</item>
    /// <item>12:47:53.00 the dockworker stands (CharacterAction 87) and is whole: HealthDamage 32, +20.</item>
    /// <item>12:47:56.14 CharacterAction 100 with 9, the same action Marcus Stone sends with 47.</item>
    /// <item>12:48:16.12 it sits down again (CharacterAction 86): HealthDamage 12, -20, melee.</item>
    /// </list>
    /// The stim's own data says what happens to one that is not wounded: "This Dockworker is not
    /// wounded." and nothing else. Its requirements ask the target's stat 62, which the requirement
    /// code here resolves against the user, so the check is made here instead.
    /// </remarks>
    public static class WoundedCharacters
    {
        /// <summary>Health Regeneration Stim, handed out by Marcus Stone.</summary>
        public const int HealthRegenerationStim = 297044;

        private static readonly TimeSpan StandsAfter = TimeSpan.FromSeconds(2);

        private static readonly TimeSpan GesturesAfter = TimeSpan.FromSeconds(3);

        private static readonly TimeSpan SitsAgainAfter = TimeSpan.FromSeconds(20);

        private const CharacterActionType StandUp = CharacterActionType.SitToggle;

        private const CharacterActionType SitDown = (CharacterActionType)86;

        private const CharacterActionType Gesture = (CharacterActionType)100;

        private const int GestureValue = 9;

        /// <summary>The ones up on their feet right now, and what they go back to.</summary>
        private static readonly ConcurrentDictionary<Identity, int> Helped = new ConcurrentDictionary<Identity, int>();

        /// <summary>
        /// Whether a character is one of the wounded: a spawned character sitting below its most.
        /// </summary>
        public static bool IsWounded(ICharacter character)
        {
            return character != null
                   && character.Controller != null
                   && character.Controller.Client == null
                   && !Helped.ContainsKey(character.Identity)
                   && character.Stats[StatIds.currentmovementmode].Value == (int)MoveModes.Sit
                   && character.Stats[StatIds.health].Value > 0
                   && character.Stats[StatIds.health].Value < character.Stats[StatIds.life].Value;
        }

        /// <summary>
        /// An item was used with a character selected.
        /// </summary>
        /// <returns>
        /// False when the use did nothing to the character, so a quest asking for it must not count it:
        /// the stim on somebody who is not wounded.
        /// </returns>
        public static bool ItemUsedOn(ICharacter user, ICharacter target, Item item)
        {
            if (item == null || item.LowID != HealthRegenerationStim)
            {
                return true;
            }

            if (!IsWounded(target))
            {
                string refusal = NotWoundedText(item);
                if (refusal != null)
                {
                    FormatFeedbackMessageHandler.Default.Send(user, "~&!!!\":!!!)<s" + (char)(refusal.Length + 1) + refusal);
                }

                return false;
            }

            Help(target);
            return true;
        }

        /// <summary>Drops whatever is remembered about a character that has left.</summary>
        public static void Forget(Identity character)
        {
            int ignored;
            Helped.TryRemove(character, out ignored);
        }

        private static void Help(ICharacter wounded)
        {
            int woundedHealth = wounded.Stats[StatIds.health].Value;
            if (!Helped.TryAdd(wounded.Identity, woundedHealth))
            {
                return;
            }

            Identity who = wounded.Identity;
            Later(
                who,
                Step(
                    StandsAfter,
                    () =>
                    {
                        ICharacter character = StillHere(wounded, who);
                        if (character == null)
                        {
                            return false;
                        }

                        int life = character.Stats[StatIds.life].Value;
                        int before = character.Stats[StatIds.health].Value;
                        character.Stats[StatIds.currentmovementmode].Value = (int)MoveModes.Run;
                        Announce(character, StandUp, 0);
                        character.Stats[StatIds.health].Value = life;
                        HealthDamageMessageHandler.Default.Send(character, life, life - before);
                        return true;
                    }),
                Step(
                    GesturesAfter,
                    () =>
                    {
                        ICharacter character = StillHere(wounded, who);
                        if (character == null)
                        {
                            return false;
                        }

                        Announce(character, Gesture, GestureValue);
                        return true;
                    }),
                Step(
                    SitsAgainAfter,
                    () =>
                    {
                        ICharacter character = StillHere(wounded, who);
                        if (character == null)
                        {
                            return false;
                        }

                        int goesBackTo = woundedHealth;
                        var playfield = character.Playfield as Playfield;
                        if (playfield != null)
                        {
                            goesBackTo = playfield.SpawnHealth(character);
                        }

                        int before = character.Stats[StatIds.health].Value;
                        character.Stats[StatIds.currentmovementmode].Value = (int)MoveModes.Sit;
                        Announce(character, SitDown, 0);
                        if (before > goesBackTo)
                        {
                            character.Stats[StatIds.health].Value = goesBackTo;
                            HealthDamageMessageHandler.Default.Send(character, goesBackTo, goesBackTo - before, DamageType.Melee);
                        }

                        return true;
                    }));
        }

        private static Tuple<TimeSpan, Func<bool>> Step(TimeSpan after, Func<bool> action)
        {
            return Tuple.Create(after, action);
        }

        /// <summary>
        /// Runs each step after the delay before it, one after the other, while each says to go on.
        /// The character is let go at the end however it ends.
        /// </summary>
        private static void Later(Identity who, params Tuple<TimeSpan, Func<bool>>[] steps)
        {
            Task.Run(
                async () =>
                {
                    try
                    {
                        foreach (Tuple<TimeSpan, Func<bool>> step in steps)
                        {
                            await Task.Delay(step.Item1);
                            if (!step.Item2())
                            {
                                return;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        LogUtil.ErrorException(exception);
                    }
                    finally
                    {
                        Forget(who);
                    }
                });
        }

        private static ICharacter StillHere(ICharacter wounded, Identity who)
        {
            if (wounded == null || wounded.Playfield == null || wounded.Stats[StatIds.health].Value <= 0)
            {
                Forget(who);
                return null;
            }

            return wounded;
        }

        private static void Announce(ICharacter character, CharacterActionType action, int value)
        {
            character.Playfield.Announce(
                new CharacterActionMessage
                {
                    Identity = character.Identity,
                    Unknown = 0,
                    Action = action,
                    Unknown1 = 0,
                    Target = new Identity { Type = IdentityType.None, Instance = value },
                    Parameter1 = 0,
                    Parameter2 = 0,
                    Unknown2 = 0
                });
        }

        /// <summary>
        /// What the item says when it is used on somebody who is not wounded: the text of its own
        /// SystemText function.
        /// </summary>
        private static string NotWoundedText(Item item)
        {
            Function text = item.Events
                .Where(e => e.EventType == EventType.OnUse)
                .SelectMany(e => e.Functions)
                .FirstOrDefault(f => f.FunctionType == (int)FunctionType.SystemText && f.Arguments.Values.Count > 0);
            return text == null ? null : text.Arguments.Values[0].AsString();
        }
    }
}
