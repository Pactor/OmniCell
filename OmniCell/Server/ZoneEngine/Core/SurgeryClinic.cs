namespace ZoneEngine.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Events;
    using OmniCell.Core.Items;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Statels;
    using OmniCell.Core.Vector;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.Playfields;

    /// <summary>
    /// The Stationary Automated Surgery Clinic: implants go in and come out only while its window is
    /// open, "5 minutes (or until you leave the playfield)".
    /// </summary>
    /// <remarks>
    /// Retail, using the Arete Landing clinic (20260909-142713 s3 7190-7195): credits drop by 5, the
    /// window text arrives, nano 157490 is cast on the user (Treatment +100 while it runs), and the
    /// use is acknowledged. That is the clinic template's own OnUse, which is run here as it is: Hit
    /// on credits (5 here, 300 for template 43553), SystemText, CastNano 157490 and the rest.
    ///
    /// The client names the clinic by a runtime statel id (Terminal:1477021820) that the playfield
    /// data does not carry, so the used terminal is matched to the nearest clinic statel within reach.
    ///
    /// Spirits are not gated: in both Shade sessions (20260914-220505, 20260915-042412) spirits went
    /// in with no clinic used, so only implants (item class 3) need the window.
    /// </remarks>
    public static class SurgeryClinic
    {
        /// <summary>Stationary Automated Surgery Clinic templates: Arete Landing, and the city one.</summary>
        private static readonly int[] ClinicTemplates = { 295742, 43553 };

        private static readonly TimeSpan WindowLength = TimeSpan.FromMinutes(5);

        /// <summary>How far from the clinic a use is accepted, in metres.</summary>
        private const double UseRange = 8.0;

        private const int ImplantItemClass = 3;

        private static readonly ConcurrentDictionary<Identity, Window> Windows = new ConcurrentDictionary<Identity, Window>();

        /// <summary>
        /// Handles a use of a terminal that is a Surgery Clinic. False when it is not one, so the
        /// caller carries on as for any other statel.
        /// </summary>
        public static bool TryUse(ICharacter character, Identity used)
        {
            StatelData clinic = Find(character, used);
            if (clinic == null)
            {
                return false;
            }

            Event onUse = clinic.Events.FirstOrDefault(e => e.EventType == EventType.OnUse);
            if (onUse == null)
            {
                return false;
            }

            // Retail's answer to a user who cannot pay is not captured; nothing happens.
            if (character.Stats[StatIds.cash].Value < Cost(onUse))
            {
                return true;
            }

            onUse.Perform(character, clinic);
            Windows[character.Identity] = new Window(character.Playfield.Identity.Instance, DateTime.UtcNow + WindowLength);
            return true;
        }

        /// <summary>Whether the character's clinic window is open here and now.</summary>
        public static bool IsOpen(ICharacter character)
        {
            Window window;
            return (character != null) && (character.Playfield != null)
                   && Windows.TryGetValue(character.Identity, out window)
                   && (window.Playfield == character.Playfield.Identity.Instance)
                   && (DateTime.UtcNow < window.Until);
        }

        /// <summary>Whether this item may be put into or taken out of an implant slot now.</summary>
        public static bool MayMoveImplant(ICharacter character, IItem item)
        {
            return (item == null) || (item.GetAttribute(76) != ImplantItemClass) || IsOpen(character);
        }

        /// <summary>Leaving the playfield closes the window.</summary>
        public static void Forget(Identity character)
        {
            Window ignored;
            Windows.TryRemove(character, out ignored);
        }

        private static StatelData Find(ICharacter character, Identity used)
        {
            if ((character == null) || (character.Playfield == null))
            {
                return null;
            }

            // The terminal the client named, through the playfield's statel runs; the nearest clinic
            // only where a playfield has no runs to name it by.
            int playfield = character.Playfield.Identity.Instance;
            StatelData named = StatelRuns.Resolve(playfield, used);
            if (named != null)
            {
                return ClinicTemplates.Contains(named.TemplateId) ? named : null;
            }

            return FindNear(playfield, character.Coordinates(), used.Type);
        }

        /// <summary>
        /// The Surgery Clinic statel nearest to a position in a playfield, within reach, or null.
        /// Only a used Terminal can be one.
        /// </summary>
        public static StatelData FindNear(int playfield, Coordinate position, IdentityType usedType)
        {
            PlayfieldData data;
            if ((usedType != IdentityType.Terminal) || (position == null)
                || !PlayfieldLoader.PFData.TryGetValue(playfield, out data))
            {
                return null;
            }

            return data.Statels
                .Where(s => s.Identity.Type == IdentityType.Terminal && ClinicTemplates.Contains(s.TemplateId))
                .Select(s => new { Statel = s, Distance = Coordinate.Distance3D(position, new Coordinate(s.X, s.Y, s.Z)) })
                .Where(x => x.Distance <= UseRange)
                .OrderBy(x => x.Distance)
                .Select(x => x.Statel)
                .FirstOrDefault();
        }

        /// <summary>The credits the clinic's OnUse takes: its Hit on stat 61 (cash).</summary>
        public static int Cost(Event onUse)
        {
            foreach (var function in onUse.Functions)
            {
                var arguments = function.Arguments.Values;
                if (((int)function.FunctionType == (int)FunctionType.Hit) && (arguments.Count >= 2)
                    && (arguments[0].AsInt32() == (int)StatIds.cash))
                {
                    return Math.Max(0, -arguments[1].AsInt32());
                }
            }

            return 0;
        }

        private sealed class Window
        {
            public Window(int playfield, DateTime until)
            {
                this.Playfield = playfield;
                this.Until = until;
            }

            public int Playfield { get; private set; }

            public DateTime Until { get; private set; }
        }
    }
}
