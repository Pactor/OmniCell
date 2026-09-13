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

namespace ZoneEngine.ChatCommands
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Statels;
    using OmniCell.Core.Vector;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;

    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Packets;
    using ZoneEngine.Core.Playfields;

    #endregion

    /// <summary>
    /// Go to a playfield by name or by number, without knowing where in it to
    /// land.
    /// </summary>
    /// <remarks>
    /// /tp already teleports, but it wants coordinates, and the coordinates of
    /// six hundred playfields are not something anybody has to hand. This asks
    /// only which playfield, and works out the arrival spot the same way the
    /// game does when a player walks through a grid exit: from the playfield's
    /// own first destination line, standing four units back from the middle of
    /// it. Somewhere a player is meant to be able to stand, in other words,
    /// rather than the origin or the middle of a wall.
    ///
    /// A playfield with no destination lines - some interiors have none - falls
    /// back to a statel, because a statel is a fixture and the floor is under
    /// it. Only if it has neither does this refuse, which is better than
    /// dropping a GM into empty space and leaving them to /tp out.
    ///
    /// Names are matched as a substring, case insensitively, and an ambiguous
    /// name lists what it matched instead of guessing. "goto arete" gets there;
    /// "goto ICC" lists the eleven playfields with ICC in the name.
    /// </remarks>
    public class ChatCommandGoto : AOChatCommand
    {
        #region Constants

        /// <summary>
        /// How many matches to print before giving up and asking for a better
        /// name. A client's chat window is not a good place for six hundred
        /// lines.
        /// </summary>
        private const int MostToList = 30;

        /// <summary>
        /// How far back from the destination line to stand, in units. The same
        /// four the grid-exit function uses.
        /// </summary>
        private const float StandBack = 4f;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="args">
        /// </param>
        /// <returns>
        /// </returns>
        public override bool CheckCommandArguments(string[] args)
        {
            return args.Length >= 2;
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        public override void CommandHelp(ICharacter character)
        {
            character.Playfield.Publish(
                ChatTextMessageHandler.Default.CreateIM(
                    character,
                    "Goes to a playfield, at the spot a player arriving there would stand.\r\n"
                    + "Usage: /goto [playfield id]\r\n" + "Or:    /goto [part of its name]\r\n"
                    + "Or:    /goto [playfield id] [which way in, from 0]\r\n"
                    + "A name that matches more than one playfield lists them instead."));
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="target">
        /// </param>
        /// <param name="args">
        /// </param>
        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            string wanted = string.Join(" ", args.Skip(1).ToArray()).Trim();
            int door = 0;

            // "goto 6553 2" is the third way in rather than the first. A name
            // cannot take one, because a name can have a number in it.
            if ((args.Length == 3) && Number(args[1]) && Number(args[2]))
            {
                wanted = args[1];
                door = int.Parse(args[2], CultureInfo.InvariantCulture);
            }

            int playfieldId;
            if (!int.TryParse(wanted, NumberStyles.Integer, CultureInfo.InvariantCulture, out playfieldId))
            {
                playfieldId = ByName(character, wanted);
                if (playfieldId == 0)
                {
                    return;
                }
            }

            if (!Playfields.ValidPlayfield(playfieldId) || !PlayfieldLoader.PFData.ContainsKey(playfieldId))
            {
                Tell(character, "There is no playfield " + playfieldId + ".");
                return;
            }

            PlayfieldData destination = PlayfieldLoader.PFData[playfieldId];
            Coordinate where;
            if (!Arrival(destination, door, out where))
            {
                Tell(
                    character,
                    destination.Name.Trim() + " (" + playfieldId + ") has no way in at number " + door
                    + ". It has " + destination.Destinations.Count + " and " + destination.Statels.Count
                    + " statels.");
                return;
            }

            Tell(
                character,
                "Going to " + destination.Name.Trim() + " (" + playfieldId + "), way in " + door + " of "
                + destination.Destinations.Count + ".");
            character.Playfield.Teleport(
                (Character)character,
                where,
                character.Heading,
                new Identity { Type = IdentityType.Playfield, Instance = playfieldId });
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public override int GMLevelNeeded()
        {
            return 1;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public override List<string> ListCommands()
        {
            return new List<string> { "goto", "gotopf" };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Where somebody arriving in this playfield should stand.
        /// </summary>
        /// <remarks>
        /// A destination is a line, not a point - the stretch of a grid exit or
        /// a zone border - so the spot is the middle of it, moved four units
        /// along its normal to put the arriving character in front of it rather
        /// than inside it. This is the same arithmetic lineteleport does, and
        /// the reason it is repeated rather than shared is that lineteleport
        /// takes its destination out of a game function's arguments.
        /// </remarks>
        private static bool Arrival(PlayfieldData playfield, int door, out Coordinate where)
        {
            where = new Coordinate();

            int seen = 0;
            foreach (PlayfieldDestination line in playfield.Destinations.Values)
            {
                float spread = WallCollision.Distance(line.StartX, line.StartZ, line.EndX, line.EndZ);
                if ((spread <= 0f) || (seen++ < door))
                {
                    continue;
                }

                float x = ((line.EndX - line.StartX) * 0.5f) + line.StartX;
                float z = ((line.EndZ - line.StartZ) * 0.5f) + line.StartZ;

                where = new Coordinate(
                    x - (((line.EndZ - line.StartZ) / spread) * StandBack),
                    line.EndY,
                    z + (((line.EndX - line.StartX) / spread) * StandBack));
                return true;
            }

            // No way in that the playfield file knows about. Anything the file
            // does place is standing on the floor, so stand there instead - but
            // only when no particular way in was asked for, because falling
            // back to a statel would not be an answer to that question.
            if ((door == 0) && (playfield.Statels.Count > 0))
            {
                StatelData statel = playfield.Statels[0];
                where = new Coordinate(statel.X, statel.Y, statel.Z);
                return true;
            }

            return false;
        }

        /// <summary>
        /// The one playfield whose name contains this, or zero when that is not
        /// one playfield.
        /// </summary>
        private static int ByName(ICharacter character, string wanted)
        {
            var found = ((Playfield)character.Playfield).ListAvailablePlayfields()
                .Where(x => x.Value.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(x => x.Value.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (found.Count == 1)
            {
                return found[0].Key.Instance;
            }

            if (found.Count == 0)
            {
                Tell(character, "No playfield has \"" + wanted + "\" in its name.");
                return 0;
            }

            var lines = new List<MessageBody>
                        {
                            ChatTextMessageHandler.Default.Create(
                                character,
                                found.Count + " playfields have \"" + wanted + "\" in the name"
                                + (found.Count > MostToList ? ", first " + MostToList : string.Empty) + ":")
                        };

            foreach (var playfield in found.Take(MostToList))
            {
                lines.Add(
                    ChatTextMessageHandler.Default.Create(
                        character,
                        playfield.Key.Instance.ToString(CultureInfo.InvariantCulture).PadLeft(8) + ": "
                        + playfield.Value.Trim()));
            }

            character.Playfield.Publish(Bulk.CreateIM(character.Controller.Client, lines.ToArray()));
            return 0;
        }

        private static bool Number(string text)
        {
            int ignored;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ignored);
        }

        private static void Tell(ICharacter character, string what)
        {
            character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, what));
        }

        #endregion
    }
}
