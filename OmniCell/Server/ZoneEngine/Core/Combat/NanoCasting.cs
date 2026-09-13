#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Combat
{
    #region Usings

    using System;
    using System.Collections.Concurrent;

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Events;
    using OmniCell.Core.Nanos;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// Nano casting, timed off the playfield heartbeat.
    /// </summary>
    /// <remarks>
    /// A cast is not instant: the client shows a bar for the attack delay, the
    /// nano lands, and the caster then cannot cast again until the recharge has
    /// run. The previous implementation expressed that with Thread.Sleep inside
    /// the message handler, which is the wrong place for it twice over - it
    /// stops that connection processing anything else for the whole cast, and
    /// it holds a pooled thread while doing nothing. A three second nano froze
    /// the caster for three seconds.
    ///
    /// The waiting is now state, checked once per heartbeat, the same way
    /// <see cref="Combat"/> schedules swings. Nothing sleeps.
    ///
    /// Casts live here rather than on ICharacter for the same reason fights do:
    /// so that adding them does not change an interface implemented across the
    /// whole server.
    /// </remarks>
    public static class NanoCasting
    {
        #region Fields

        /// <summary>
        /// Casts in flight, by the caster's identity.
        /// </summary>
        private static readonly ConcurrentDictionary<Identity, Cast> Casting =
            new ConcurrentDictionary<Identity, Cast>();

        /// <summary>
        /// When each caster may start another cast.
        /// </summary>
        private static readonly ConcurrentDictionary<Identity, DateTime> Recharging =
            new ConcurrentDictionary<Identity, DateTime>();

        /// <summary>
        /// Nanos currently running, by the character they are running on.
        /// </summary>
        private static readonly ConcurrentDictionary<Identity, List<Running>> Landed =
            new ConcurrentDictionary<Identity, List<Running>>();

        /// <summary>
        /// Nano cost, in the item attribute numbering the nano files use.
        /// </summary>
        private const int NanoCostAttribute = 407;

        /// <summary>
        /// Nano duration.
        /// </summary>
        private const int DurationAttribute = 8;

        /// <summary>
        /// Recharge delay, in hundredths of a second.
        /// </summary>
        private const int RechargeAttribute = 210;

        /// <summary>
        /// What CalculateNanoAttackTime returns when it cannot work one out.
        /// </summary>
        private const int NoAttackTime = 1234567890;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Begins a cast, if the caster can make it.
        /// </summary>
        /// <returns>
        /// True when the cast started. False when it was refused, in which case
        /// the caster has been told why.
        /// </returns>
        public static bool Start(ICharacter caster, int nanoId, Identity target)
        {
            NanoFormula nano;
            if (!NanoLoader.NanoList.TryGetValue(nanoId, out nano))
            {
                // Casting an unknown id used to index the dictionary directly and
                // throw, taking the connection down with it.
                Console.WriteLine(
                    "Character " + caster.Identity.Instance + " tried to cast nano " + nanoId
                    + ", which is not in nanos.ocp.");
                return false;
            }

            if (Casting.ContainsKey(caster.Identity))
            {
                return false;
            }

            DateTime rechargedAt;
            if (Recharging.TryGetValue(caster.Identity, out rechargedAt) && DateTime.UtcNow < rechargedAt)
            {
                return false;
            }

            int cost = nano.getItemAttribute(NanoCostAttribute);
            if (caster.Stats[StatIds.currentnano].Value < cost)
            {
                // The client does not stop this by itself - it sends the cast and
                // waits to be told. Saying nothing leaves the cast bar running
                // until the character is relogged.
                CharacterActionMessageHandler.Default.FinishNanoCasting(
                    caster,
                    CharacterActionType.FinishNanoCasting,
                    Identity.None,
                    0,
                    nanoId);
                return false;
            }

            int attackTime = caster.CalculateNanoAttackTime(nano);
            TimeSpan castTime = attackTime == NoAttackTime
                                    ? TimeSpan.Zero
                                    : TimeSpan.FromMilliseconds(attackTime * 10);

            CastNanoSpellMessageHandler.Default.Send(caster, nanoId, target);

            Casting[caster.Identity] = new Cast(nanoId, target, cost, DateTime.UtcNow + castTime);
            return true;
        }

        /// <summary>
        /// Abandons a cast in progress.
        /// </summary>
        public static void Stop(ICharacter caster)
        {
            Cast ignored;
            Casting.TryRemove(caster.Identity, out ignored);
        }

        /// <summary>
        /// Whether this character is part way through a cast.
        /// </summary>
        public static bool IsCasting(ICharacter caster)
        {
            return Casting.ContainsKey(caster.Identity);
        }

        /// <summary>
        /// Finishes any cast whose time has come. Called once per heartbeat.
        /// </summary>
        public static void Tick(ICharacter caster)
        {
            if (caster == null)
            {
                return;
            }

            Expire(caster);

            Cast cast;
            if (!Casting.TryGetValue(caster.Identity, out cast))
            {
                return;
            }

            if (DateTime.UtcNow < cast.LandsAt)
            {
                return;
            }

            Cast finished;
            if (!Casting.TryRemove(caster.Identity, out finished))
            {
                // Another heartbeat took it first.
                return;
            }

            NanoFormula nano;
            if (!NanoLoader.NanoList.TryGetValue(cast.NanoId, out nano))
            {
                return;
            }

            caster.Stats[StatIds.currentnano].Value =
                Math.Max(0, caster.Stats[StatIds.currentnano].Value - cast.Cost);
            caster.SendChangedStats();

            CharacterActionMessageHandler.Default.FinishNanoCasting(
                caster,
                CharacterActionType.FinishNanoCasting,
                Identity.None,
                1,
                cast.NanoId);

            // Retail lands the effect (and announces any Buff) after
            // FinishNanoCasting and before SetNanoDuration.
            Land(caster, nano, cast.Target);

            CharacterActionMessageHandler.Default.SetNanoDuration(
                caster,
                cast.Target,
                cast.NanoId,
                nano.getItemAttribute(DurationAttribute));

            int recharge = nano.getItemAttribute(RechargeAttribute);
            if (recharge > 0)
            {
                Recharging[caster.Identity] = DateTime.UtcNow + TimeSpan.FromMilliseconds(recharge * 10);
            }
        }

        /// <summary>
        /// Drops everything remembered about a character.
        /// </summary>
        /// <remarks>
        /// Called when a character leaves the playfield. Without it a
        /// disconnected caster's entry sits in both dictionaries for the life of
        /// the process.
        /// </remarks>
        public static void Forget(Identity caster)
        {
            Cast cast;
            Casting.TryRemove(caster, out cast);
            DateTime recharge;
            Recharging.TryRemove(caster, out recharge);
            List<Running> running;
            Landed.TryRemove(caster, out running);
        }

        #endregion

        #region Effects

        /// <summary>
        /// Applies a nano's effect to whoever it was cast on.
        /// </summary>
        /// <remarks>
        /// A nano formula carries its effect as an OnUse event and its undo as an
        /// OnTerminate event - 9257 of the 10815 nanos in nanos.dat have the
        /// first and 1609 have the second. Running those is the whole of a nano
        /// doing something, and neither was ever run: casting reported a nano
        /// landing and then changed nothing about anybody.
        ///
        /// The functions are performed through the same path an item's events
        /// take, so Modify, Set and Hit behave here exactly as they do when a
        /// piece of armour is worn. Function types with no implementation yet -
        /// Skill and the summon and area cast families, mostly - log themselves
        /// as unimplemented and the rest of the nano still applies.
        ///
        /// Anarchy Online allows one nano per strain, a newer cast replacing an
        /// older, which is why an existing nano of the same strain is terminated
        /// first rather than stacked on top of.
        /// </remarks>
        private static void Land(ICharacter caster, NanoFormula nano, Identity targetIdentity)
        {
            ICharacter target = Resolve(caster, targetIdentity);
            if (target == null)
            {
                return;
            }

            int strain = nano.NanoStrain();
            Terminate(target, strain);

            // Health before, so that a nano which heals or hurts can say by how
            // much. A weapon swing reports itself through AttackInfo; this is
            // the other way health moves, and HealthDamage is the message for it.
            int healthBefore = target.Stats[StatIds.health].Value;

            foreach (Event ev in nano.Events.Where(e => e.EventType == EventType.OnUse))
            {
                ev.Perform(target, caster);
            }

            int healthAfter = target.Stats[StatIds.health].Value;
            if (healthAfter != healthBefore)
            {
                HealthDamageMessageHandler.Default.Send(target, healthAfter, healthAfter - healthBefore);
            }

            int duration = nano.getItemAttribute(DurationAttribute);
            if (duration > 0)
            {
                var running = new Running(nano.ID, strain, DateTime.UtcNow + TimeSpan.FromMilliseconds(duration * 10));

                Landed.AddOrUpdate(
                    target.Identity,
                    id => new List<Running> { running },
                    (id, list) =>
                        {
                            lock (list)
                            {
                                list.Add(running);
                            }

                            return list;
                        });

                // Fully qualified: SmokeLounge's GameData has an ActiveNano too,
                // and that one is the wire form of this.
                BuffMessageHandler.Default.Applied(target, nano.ID);

                target.ActiveNanos[strain] = new OmniCell.Core.Nanos.ActiveNano
                                             {
                                                 ID = nano.ID,
                                                 Instance = nano.Instance,
                                                 TickCounter = duration,
                                                 TickInterval = duration
                                             };
            }

            target.SendChangedStats();
        }

        /// <summary>
        /// Ends any nano on this character whose duration has run out.
        /// </summary>
        private static void Expire(ICharacter character)
        {
            List<Running> running;
            if (!Landed.TryGetValue(character.Identity, out running))
            {
                return;
            }

            List<Running> done;
            lock (running)
            {
                done = running.Where(r => DateTime.UtcNow >= r.EndsAt).ToList();
                foreach (Running r in done)
                {
                    running.Remove(r);
                }
            }

            foreach (Running r in done)
            {
                RunTerminate(character, r.NanoId);
                character.ActiveNanos.Remove(r.Strain);
                BuffMessageHandler.Default.Removed(character, r.NanoId);
            }

            if (done.Count > 0)
            {
                character.SendChangedStats();
            }
        }

        /// <summary>
        /// Ends the nano of a given strain, if one is running.
        /// </summary>
        private static void Terminate(ICharacter character, int strain)
        {
            if (!character.ActiveNanos.ContainsKey(strain))
            {
                return;
            }

            int nanoId = character.ActiveNanos[strain].ID;
            character.ActiveNanos.Remove(strain);
            BuffMessageHandler.Default.Removed(character, nanoId);

            List<Running> running;
            if (Landed.TryGetValue(character.Identity, out running))
            {
                lock (running)
                {
                    running.RemoveAll(r => r.Strain == strain);
                }
            }

            RunTerminate(character, nanoId);
        }

        /// <summary>
        /// Runs a nano's OnTerminate event.
        /// </summary>
        /// <remarks>
        /// This is how a nano is undone. It is the nano's own data that says what
        /// to reverse, rather than the server remembering what it did, which
        /// means a nano whose effect the server does not implement is also not
        /// wrongly un-applied.
        /// </remarks>
        private static void RunTerminate(ICharacter character, int nanoId)
        {
            NanoFormula nano;
            if (!NanoLoader.NanoList.TryGetValue(nanoId, out nano))
            {
                return;
            }

            foreach (Event ev in nano.Events.Where(e => e.EventType == EventType.OnTerminate))
            {
                ev.Perform(character, character);
            }
        }

        /// <summary>
        /// Finds who a nano was cast on.
        /// </summary>
        /// <remarks>
        /// A self cast carries no target, or carries the caster's own identity.
        /// Anything else is looked up in the caster's playfield; a target that
        /// has left in the time the cast took resolves to nothing and the nano
        /// simply does not land.
        /// </remarks>
        private static ICharacter Resolve(ICharacter caster, Identity target)
        {
            if (target.Instance == 0 || target.Instance == caster.Identity.Instance)
            {
                return caster;
            }

            return caster.Playfield == null ? null : caster.Playfield.FindByIdentity<ICharacter>(target);
        }

        #endregion

        /// <summary>
        /// One cast in progress.
        /// </summary>
        private class Cast
        {
            public Cast(int nanoId, Identity target, int cost, DateTime landsAt)
            {
                this.NanoId = nanoId;
                this.Target = target;
                this.Cost = cost;
                this.LandsAt = landsAt;
            }

            public int NanoId { get; private set; }

            public Identity Target { get; private set; }

            public int Cost { get; private set; }

            public DateTime LandsAt { get; private set; }
        }

        /// <summary>
        /// One nano running on a character.
        /// </summary>
        private class Running
        {
            public Running(int nanoId, int strain, DateTime endsAt)
            {
                this.NanoId = nanoId;
                this.Strain = strain;
                this.EndsAt = endsAt;
            }

            public int NanoId { get; private set; }

            public int Strain { get; private set; }

            public DateTime EndsAt { get; private set; }
        }
    }
}
