#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.ChatCommands
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Statels;
    using OmniCell.Core.Vector;
    using OmniCell.Database.Dao;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;

    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Packets;
    using ZoneEngine.Core.Playfields;

    #endregion

    /// <summary>
    /// Says which playfield you are standing in, and what the server thinks is
    /// in it.
    /// </summary>
    /// <remarks>
    /// Written because working out where somebody was took a packet capture
    /// and an afternoon. A building looks like the building it is modelled on
    /// whichever playfield it belongs to, so "this is Fair Trade" and "this is
    /// playfield 1186" are different claims and only the second one can be
    /// checked. This prints the second.
    ///
    /// The shop counts are here for the same reason: a machine with nothing on
    /// its shelves looks exactly like a machine the server has never heard of,
    /// and the difference is one number.
    /// </remarks>
    public class ChatCommandWhere : AOChatCommand
    {
        #region Public Methods and Operators

        public override bool CheckCommandArguments(string[] args)
        {
            return true;
        }

        public override void CommandHelp(ICharacter character)
        {
            character.Playfield.Publish(
                ChatTextMessageHandler.Default.CreateIM(
                    character,
                    "Usage: /command where - the playfield you are in, and what is in it"));
        }

        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            int playfield = character.Playfield.Identity.Instance;
            Coordinate at = character.Coordinates();
            var lines = new List<MessageBody>();

            PlayfieldData data;
            string name = PlayfieldLoader.PFData.TryGetValue(playfield, out data) && data != null
                              ? data.Name
                              : "no playfield data loaded";

            Say(lines, character, string.Format("playfield {0} - {1}", playfield, name));
            Say(
                lines,
                character,
                string.Format("standing at {0:0.##} {1:0.##} {2:0.##}", at.x, at.y, at.z));

            if (data != null)
            {
                var machines = data.Statels
                    .Where(s => s.Identity.Type == IdentityType.VendingMachine)
                    .ToList();

                Say(
                    lines,
                    character,
                    string.Format(
                        "{0} statels here, {1} of them shop machines, {2} doors",
                        data.Statels.Count,
                        machines.Count,
                        data.Statels.Count(s => (int)s.Identity.Type == DoorType)));

                Stocked(lines, character, machines);
                Nearest(lines, character, data, at);
            }

            var vendors = Pool.Instance.GetAll<Vendor>(character.Playfield.Identity).ToList();
            Say(
                lines,
                character,
                string.Format(
                    "{0} shops are live here, {1} of them with something for sale",
                    vendors.Count,
                    vendors.Count(v => Holding(v) > 0)));

            character.Playfield.Publish(Bulk.CreateIM(character.Controller.Client, lines.ToArray()));
        }

        public override List<string> ListCommands()
        {
            return new List<string> { "where", "whereami" };
        }

        public override int GMLevelNeeded()
        {
            return 1;
        }

        #endregion

        #region Methods

        private const int DoorType = 51016;

        /// <summary>
        /// How many of this playfield's machines the database has stock for,
        /// which is the question when a shop opens empty.
        /// </summary>
        private static void Stocked(
            List<MessageBody> lines,
            ICharacter character,
            List<StatelData> machines)
        {
            if (machines.Count == 0)
            {
                return;
            }

            int known = 0;
            foreach (int template in machines.Select(m => m.TemplateId).Distinct())
            {
                string hash = OmniCell.Core.VendorHandler.VendorHandler.MachineHash(template);
                if (VendorTemplateDao.Instance.GetWhere(new { Hash = hash }).Any())
                {
                    known++;
                }
            }

            Say(
                lines,
                character,
                string.Format(
                    "{0} of {1} machine kinds have stock in the database",
                    known,
                    machines.Select(m => m.TemplateId).Distinct().Count()));
        }

        /// <summary>
        /// The statel you are standing closest to, and how far off it is.
        /// </summary>
        private static void Nearest(
            List<MessageBody> lines,
            ICharacter character,
            PlayfieldData data,
            Coordinate at)
        {
            StatelData closest = null;
            double best = double.MaxValue;
            foreach (StatelData s in data.Statels)
            {
                double d = s.Coord().Distance3D(at);
                if (d < best)
                {
                    best = d;
                    closest = s;
                }
            }

            if (closest == null)
            {
                return;
            }

            Say(
                lines,
                character,
                string.Format(
                    "nearest statel {0} type {1} template {2}, {3:0.#} away",
                    closest.Identity.Instance,
                    (int)closest.Identity.Type,
                    closest.TemplateId,
                    best));
        }

        private static int Holding(Vendor vendor)
        {
            try
            {
                return vendor.BaseInventory.Pages[vendor.BaseInventory.StandardPage].List().Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static void Say(List<MessageBody> lines, ICharacter character, string text)
        {
            lines.Add(ChatTextMessageHandler.Default.Create(character, text));
        }

        #endregion
    }
}
