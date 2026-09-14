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

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;
    using OmniCell.Stats;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.Loot;
    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// Melee and ranged auto attack.
    /// </summary>
    /// <remarks>
    /// The client opens a fight by sending AttackMessage once and then says
    /// nothing further. Every swing after that is the server's to schedule and
    /// report, which is why this runs off the playfield heartbeat rather than
    /// off anything the client sends.
    ///
    /// Fight state lives here rather than on ICharacter so that adding combat
    /// does not change an interface implemented across the whole server. The
    /// entry is removed when the fight stops, the target dies, or the attacker
    /// leaves the playfield.
    /// </remarks>
    public static class Combat
    {
        #region Fields

        /// <summary>
        /// Attackers, by their identity.
        /// </summary>
        private static readonly ConcurrentDictionary<Identity, Fight> Fights =
            new ConcurrentDictionary<Identity, Fight>();

        /// <summary>
        /// Shared source of randomness for damage rolls.
        /// </summary>
        /// <remarks>
        /// One instance, locked on use. Creating a Random per roll seeds from
        /// the clock, and rolls made in the same tick then come out identical -
        /// which in a combat loop means every swing in a round hitting for the
        /// same number.
        /// </remarks>
        private static readonly Random Rng = new Random();

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts <paramref name="attacker"/> attacking <paramref name="target"/>.
        /// </summary>
        /// <returns>
        /// False if the target cannot be attacked, in which case nothing changed.
        /// </returns>
        public static bool Start(ICharacter attacker, Identity target)
        {
            if (attacker == null || attacker.Playfield == null)
            {
                return false;
            }

            ICharacter victim = attacker.Playfield.FindByIdentity<ICharacter>(target);
            if (victim == null || victim.Identity == attacker.Identity)
            {
                return false;
            }

            if (IsDead(victim))
            {
                return false;
            }

            // Retail announces the fight before its first result. Giving the
            // client one weapon-delay interval between AttackMessage and the
            // first AttackInfo both preserves that packet order and gives the
            // visible attack state time to begin before damage can kill a
            // low-health target.
            var fight = new Fight(target) { NextSwing = DateTime.UtcNow + AttackDelay(attacker) };
            Fights[attacker.Identity] = fight;
            attacker.Controller.State = CharacterState.Fighting;
            SpecialAttackWeaponMessageHandler.Default.AnnounceCombatStart(attacker);
            AttackMessageHandler.Default.Send(attacker, target);
            return true;
        }

        /// <summary>
        /// Stops <paramref name="attacker"/> fighting, if they were.
        /// </summary>
        public static void Stop(ICharacter attacker)
        {
            if (attacker == null)
            {
                return;
            }

            Fight ignored;
            if (Fights.TryRemove(attacker.Identity, out ignored) && attacker.Controller != null
                && attacker.Controller.State == CharacterState.Fighting)
            {
                attacker.Controller.State = CharacterState.Idle;
            }
        }

        /// <summary>
        /// True while <paramref name="attacker"/> has a fight running.
        /// </summary>
        public static bool IsFighting(ICharacter attacker)
        {
            return attacker != null && Fights.ContainsKey(attacker.Identity);
        }

        /// <summary>
        /// Runs one heartbeat's worth of combat for <paramref name="attacker"/>.
        /// </summary>
        /// <remarks>
        /// Called for every character each heartbeat, so the common case - not
        /// fighting - returns immediately.
        /// </remarks>
        public static void Tick(ICharacter attacker)
        {
            Fight fight;
            if (attacker == null || !Fights.TryGetValue(attacker.Identity, out fight))
            {
                return;
            }

            if (DateTime.UtcNow < fight.NextSwing)
            {
                return;
            }

            if (attacker.Playfield == null || IsDead(attacker))
            {
                Stop(attacker);
                return;
            }

            ICharacter victim = attacker.Playfield.FindByIdentity<ICharacter>(fight.Target);
            if (victim == null || IsDead(victim))
            {
                // Target gone or already down. Nothing to report; the death was
                // announced when the killing blow landed.
                Stop(attacker);
                StopFightMessageHandler.Default.Send(attacker);
                return;
            }

            Swing(attacker, victim);
            fight.NextSwing = DateTime.UtcNow + AttackDelay(attacker);
        }

        /// <summary>
        /// Uses a special attack against a target.
        /// </summary>
        /// <remarks>
        /// A special is a single extra blow on demand, on its own recharge,
        /// alongside the ordinary swing loop - burst, fling, aimed shot, brawl.
        /// The client asks for one by sending CharSecSpecAttack naming the skill,
        /// and the server answers with the same message and then a
        /// SpecialAttackInfo saying what it did.
        ///
        /// The recharge is not enforced yet, which matters: the client greys its
        /// own buttons out while a special recharges, so the only way to fire one
        /// early is to ask for it deliberately. That is a hole, and it is named
        /// here rather than left to be discovered.
        ///
        /// Damage is the ordinary swing damage. Anarchy Online scales each
        /// special differently off its own skill, and none of those formulas are
        /// derivable from the captures - they show damage that landed, not how
        /// it was arrived at.
        /// </remarks>
        public static bool UseSpecial(ICharacter attacker, Identity target, int skill)
        {
            if (attacker == null || attacker.Playfield == null || IsDead(attacker))
            {
                return false;
            }

            ICharacter victim = attacker.Playfield.FindByIdentity<ICharacter>(target);
            if (victim == null || IsDead(victim))
            {
                return false;
            }

            CharSecSpecAttackMessageHandler.Default.Send(attacker, target, skill);
            int weaponSlot = EquippedWeaponSlot(attacker);

            if (!Hits(attacker, victim))
            {
                MissedAttackInfoMessageHandler.Default.Send(attacker, target, weaponSlot, skill);
                return true;
            }

            int damage = RollDamage(attacker, victim);
            int health = victim.Stats[StatIds.health].Value - damage;
            victim.Stats[StatIds.health].Value = Math.Max(0, health);

            SpecialAttackInfoMessageHandler.Default.Send(attacker, target, damage, skill, weaponSlot);
            victim.SendChangedStats();

            if (health <= 0)
            {
                Kill(attacker, victim);
            }

            return true;
        }

        /// <summary>
        /// Drops any fight state for a character that is leaving.
        /// </summary>
        public static void Forget(Identity attacker)
        {
            Fight ignored;
            Fights.TryRemove(attacker, out ignored);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Resolves and reports one swing.
        /// </summary>
        private static void Swing(ICharacter attacker, ICharacter victim)
        {
            int weaponSlot = EquippedWeaponSlot(attacker);
            // Being hit is a reason to hit back, and it is the only reason a
            // spawned character has ever needed. The captures are full of
            // AttackInfo whose sender is a monster: a Cleaning Robot swings at
            // whoever swung at it, on the same clock as everybody else, and
            // there was nothing here making that happen.
            //
            // Whoever struck first keeps the fight. A mob already fighting
            // somebody else is not distracted by a second attacker, which is
            // both simpler and how the game behaves.
            FightBack(victim, attacker);

            if (!Hits(attacker, victim))
            {
                MissedAttackInfoMessageHandler.Default.Send(attacker, victim.Identity, weaponSlot);
                return;
            }

            int damage = RollDamage(attacker, victim);
            int health = victim.Stats[StatIds.health].Value - damage;
            victim.Stats[StatIds.health].Value = Math.Max(0, health);

            AttackInfoMessageHandler.Default.Send(attacker, victim.Identity, damage, weaponSlot);
            victim.SendChangedStats();

            if (health <= 0)
            {
                Kill(attacker, victim);
            }
        }

        /// <summary>
        /// A spawned character that has been attacked attacks back.
        /// </summary>
        /// <remarks>
        /// Players are left alone: a player decides for itself what it is doing,
        /// and being hit is not consent to start swinging.
        /// </remarks>
        private static void FightBack(ICharacter victim, ICharacter attacker)
        {
            if (victim == null || attacker == null || victim.Identity == attacker.Identity)
            {
                return;
            }

            if (victim.Controller == null || !(victim.Controller is NPCController))
            {
                return;
            }

            if (IsFighting(victim) || IsDead(victim))
            {
                return;
            }

            Start(victim, attacker.Identity);
        }

        /// <summary>
        /// Whether this swing connects.
        /// </summary>
        /// <remarks>
        /// Resolved from the attacker's attack rating against the defender's
        /// defense rating, which is the shape Anarchy Online uses. The exact
        /// curve Funcom applies between the two is not published and is not
        /// derivable from the captures on hand - they show damage that landed,
        /// not the roll behind it - so the mapping from the difference to a
        /// percentage is an approximation, stated here rather than buried:
        ///
        ///   equal ratings          50%
        ///   every point of lead    +1%, and every point behind -1%
        ///   never below 5%, never above 95%
        ///
        /// Monotonic, symmetric, and bounded so that neither side becomes
        /// untouchable, which is the part that matters for the loop being
        /// playable. Replacing it means changing this method and nothing else.
        /// </remarks>
        private static bool Hits(ICharacter attacker, ICharacter victim)
        {
            int chance = 50 + (AttackRating(attacker) - DefenseRating(victim));
            chance = Math.Max(5, Math.Min(95, chance));

            lock (Rng)
            {
                return Rng.Next(100) < chance;
            }
        }

        /// <summary>
        /// What the attacker brings to the roll.
        /// </summary>
        /// <remarks>
        /// Attack rating in Anarchy Online belongs to the weapon, not the
        /// wielder. A weapon names the skills it is used with, each with a
        /// percentage, and the rating is those skills weighted by those
        /// percentages - which is why the same character is better with a pistol
        /// than with a rifle. That is read here off the equipped weapon.
        ///
        /// Falling back, in order:
        ///
        ///   equipped weapon    its own skills, weighted
        ///   bare handed        martial arts and brawl, the unarmed skills
        ///   neither set        level
        ///
        /// The last case is what a spawned NPC hits: it has no inventory and no
        /// skills, only the stats the capture described. Mob attack ratings in
        /// Anarchy Online track level closely, so level keeps a level 3
        /// dockworker and a level 158 colonist in sensible relation to each
        /// other and to a player of matching level.
        /// </remarks>
        private static int AttackRating(ICharacter attacker)
        {
            IItem weapon = EquippedWeapon(attacker);
            if (weapon != null)
            {
                int rating = 0;
                int weighted = 0;

                foreach (KeyValuePair<int, int> skill in weapon.AttackSkills)
                {
                    rating += attacker.Stats[skill.Key].Value * skill.Value;
                    weighted += skill.Value;
                }

                // The percentages should add to 100, but an item whose data says
                // otherwise is normalised rather than trusted - a weapon listing
                // 50% of one skill would otherwise halve its wielder's rating.
                if (weighted > 0)
                {
                    return Math.Max(1, rating / weighted);
                }
            }

            int unarmed = Math.Max(
                attacker.Stats[StatIds.martialarts].Value,
                attacker.Stats[StatIds.brawl].Value);

            return unarmed > 0 ? unarmed : Math.Max(1, attacker.Stats[StatIds.level].Value);
        }

        /// <summary>
        /// The weapon this character is holding, if any.
        /// </summary>
        /// <remarks>
        /// The weapon page holds both hands and the ranged and melee slots at
        /// once. The first item found is used, which for the usual case of one
        /// weapon is that weapon. Choosing correctly between two of them needs
        /// the attack to say which hand it came from, and nothing sends that
        /// yet.
        ///
        /// An NPC has no inventory at all, so this is null for every spawned
        /// mob.
        /// </remarks>
        private static IItem EquippedWeapon(ICharacter attacker)
        {
            if (attacker.BaseInventory == null)
            {
                return null;
            }

            IInventoryPage page = attacker.BaseInventory[(int)IdentityType.WeaponPage];
            if (page == null)
            {
                return null;
            }

            foreach (KeyValuePair<int, IItem> slot in page.List())
            {
                if (CombatWeaponProfiles.IsPlayerWeaponSlot(slot.Key)
                    && slot.Value != null && slot.Value.AttackSkills.Count > 0)
                {
                    return slot.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// The actual equipped placement used by the client's combat holder.
        /// </summary>
        private static int EquippedWeaponSlot(ICharacter attacker)
        {
            if (attacker.BaseInventory != null)
            {
                IInventoryPage page = attacker.BaseInventory[(int)IdentityType.WeaponPage];
                if (page != null)
                {
                    foreach (KeyValuePair<int, IItem> slot in page.List())
                    {
                        if (CombatWeaponProfiles.IsPlayerWeaponSlot(slot.Key)
                            && slot.Value != null && slot.Value.AttackSkills.Count > 0)
                        {
                            return slot.Key;
                        }
                    }
                }
            }

            var playfield = attacker.Playfield as OmniCell.Core.Playfields.Playfield;
            if (playfield != null)
            {
                foreach (var weapon in playfield.WeaponsOf(attacker.Identity))
                {
                    if (weapon.BodyLocation.HasValue)
                    {
                        return weapon.BodyLocation.Value;
                    }
                }
            }

            return CombatWeaponProfiles.DefaultAttackSlot;
        }

        /// <summary>
        /// What the defender brings to the roll.
        /// </summary>
        /// <remarks>
        /// The best of the three evade skills. Anarchy Online picks one of them
        /// according to how it is being attacked - close combat against evade
        /// close, ranged against dodge or duck - and picking the best of the
        /// three is generous to the defender by comparison. It is chosen over
        /// picking the wrong one, which OmniCell would have to do while it has
        /// no weapon type to attack with.
        ///
        /// As with the attacker, an NPC with no evades set falls back to level.
        /// </remarks>
        private static int DefenseRating(ICharacter victim)
        {
            int best = Math.Max(
                victim.Stats[StatIds.evade].Value,
                Math.Max(victim.Stats[StatIds.dodge].Value, victim.Stats[StatIds.duck].Value));

            return best > 0 ? best : Math.Max(1, victim.Stats[StatIds.level].Value);
        }

        /// <summary>
        /// Rolls damage for one swing, after the defender's armour.
        /// </summary>
        /// <remarks>
        /// Weapon damage plus the attacker's damage modifier, less the
        /// defender's armour class for the damage type. Armour class in Anarchy
        /// Online subtracts a tenth of its value from each hit, and a hit that
        /// lands always does at least 1, which is why the floor is here and not
        /// a clamp to zero.
        ///
        /// Melee armour is used for every attack, because there is no weapon to
        /// ask for a damage type yet. That is the same limitation
        /// AttackRating has and will be fixed in the same place.
        ///
        /// A character with no weapon damage set falls back to a small fixed
        /// range rather than dealing nothing, so a fist fight still resolves.
        /// </remarks>
        private static int RollDamage(ICharacter attacker, ICharacter victim)
        {
            // The weapon's numbers, then the character's, then a bare fist.
            //
            // A Solar-Powered Pistol carries mindamage 2 and maxdamage 18 of its
            // own; the character carries neither until something puts them there.
            // Reading only the character meant every swing rolled the stat list's
            // unset marker, and the client was told a hit for 1234567890.
            IItem weapon = EquippedWeapon(attacker);

            int min = StatValue.Or(
                weapon != null ? weapon.GetAttribute((int)StatIds.mindamage) : StatValue.Unset,
                StatValue.Or(attacker.Stats[StatIds.mindamage].Value, 1));
            int max = StatValue.Or(
                weapon != null ? weapon.GetAttribute((int)StatIds.maxdamage) : StatValue.Unset,
                StatValue.Or(attacker.Stats[StatIds.maxdamage].Value, 5));

            if (max <= 0 || max < min)
            {
                min = 1;
                max = 5;
            }

            int rolled;
            lock (Rng)
            {
                rolled = Rng.Next(min, max + 1);
            }

            rolled += StatValue.OrZero(attacker.Stats[StatIds.meleedamagemodifier].Value);
            rolled -= StatValue.OrZero(victim.Stats[StatIds.meleeac].Value) / 10;

            return Math.Max(1, rolled);
        }



        /// <summary>
        /// How long until this attacker may swing again.
        /// </summary>
        /// <remarks>
        /// Attack speed in Anarchy Online comes from the equipped weapon and is
        /// expressed in hundredths of a second. Where that is not set a flat
        /// second is used, which is slow but keeps the loop legible while it is
        /// being tested.
        /// </remarks>
        private static TimeSpan AttackDelay(ICharacter attacker)
        {
            // Also the weapon's. A pistol says itemdelay 100, which is a second,
            // and the character says nothing at all - so this used to work out a
            // delay from the unset marker and schedule the next swing for four
            // months' time.
            IItem weapon = EquippedWeapon(attacker);
            int delay = StatValue.Or(
                weapon != null ? weapon.GetAttribute((int)StatIds.itemdelay) : StatValue.Unset,
                StatValue.Or(attacker.Stats[StatIds.attackspeed].Value, 100));

            if (delay <= 0)
            {
                delay = 100;
            }

            // Hundredths of a second, kept inside something a fight can be
            // watched at either end of.
            return TimeSpan.FromMilliseconds(Math.Min(10000, Math.Max(200, delay * 10)));
        }

        /// <summary>
        /// Handles the killing blow.
        /// </summary>
        private static void Kill(ICharacter attacker, ICharacter victim)
        {
            victim.Stats[StatIds.health].Value = 0;
            Stop(attacker);
            StopFightMessageHandler.Default.Send(attacker);

            // Quests that are counting this kind of kill hear about it here.
            // Nothing else in the server knows a mob has died.
            QuestManager.OnKill(attacker, victim);

            // And something has to be left behind, or the kill produces nothing
            // at all - no body, nothing to loot.
            Identity corpse = CorpseFullUpdateMessageHandler.Default.Send(victim);

            // The wire object and its inventory are separate things.  Keep the
            // inventory in the object pool so opening the corpse and moving an
            // item out of it follow the same path as every other container.
            CorpseLoot previous = Pool.Instance.GetObject<CorpseLoot>(victim.Playfield.Identity, corpse);
            if (previous != null)
            {
                previous.Dispose();
            }

            var loot = new CorpseLoot(victim.Playfield.Identity, corpse);

            // Loot comes from database rows. A bad row or a database error must not stop the rest of
            // the kill - the corpse and, above all, telling the playfield so the mob respawns.
            try
            {
                LootGenerator.Fill(loot, victim);
            }
            catch (System.Exception e)
            {
                global::Utility.LogUtil.ErrorException(e, "Loot for {0} could not be generated", victim.Name);
            }

            // The corpse is the model; this is the container that makes it
            // clickable. The live server sends both.
            ChestItemFullUpdateMessageHandler.Default.SendForCorpse(
                victim,
                corpse,
                victim.Stats[StatIds.cash].Value);

            // And the playfield is told, because it owns the spawn point this
            // one came from and it is the only thing that can put another one
            // there. Without this a killed character stays dead where it fell
            // for as long as the zone is up.
            var playfield = victim.Playfield as OmniCell.Core.Playfields.Playfield;
            if (playfield != null)
            {
                playfield.Died(victim);
            }

            // Anything that was attacking the corpse should stop too, otherwise
            // it keeps swinging at a dead target every heartbeat.
            foreach (var entry in Fights)
            {
                if (entry.Value.Target == victim.Identity)
                {
                    Fight ignored;
                    Fights.TryRemove(entry.Key, out ignored);
                }
            }
        }

        private static bool IsDead(ICharacter character)
        {
            return character.Stats[StatIds.health].Value <= 0;
        }

        #endregion

        /// <summary>
        /// One attacker's running fight.
        /// </summary>
        private class Fight
        {
            public Fight(Identity target)
            {
                this.Target = target;
            }

            /// <summary>
            /// Who is being attacked.
            /// </summary>
            public Identity Target { get; private set; }

            /// <summary>
            /// When the next swing is due.
            /// </summary>
            public DateTime NextSwing { get; set; }
        }
    }
}
