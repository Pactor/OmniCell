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

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using OmniCell.Core.Components;
    using OmniCell.Core.Network;
    using OmniCell.Core.Vector;
    using ZoneEngine.Core.Quests;


    using NLog;

    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.InternalMessages;

    using Vector3 = SmokeLounge.AOtomation.Messaging.GameData.Vector3;

    #endregion

    /// <summary>
    /// </summary>
    // Only inbound? Maybe we need outbound too, because of Fear nanos - Algorithman
    [MessageHandler(MessageHandlerDirection.InboundOnly)]
    public class CharDCMoveMessageHandler : BaseMessageHandler<CharDCMoveMessage, CharDCMoveMessageHandler>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// </summary>
        public CharDCMoveMessageHandler()
        {
            // Movement has its own message and does not change character stats.
            // Flushing the entire changed-stat set here attached unrelated
            // regeneration state (health, nano and their deltas) to the first
            // movement packet. Regeneration already publishes its changes from
            // the playfield heartbeat, where those changes actually occur.
            this.UpdateCharacterStatsOnReceive = false;
        }

        #region Inbound

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <param name="client">
        /// </param>
        protected override void Read(CharDCMoveMessage message, IZoneClient client)
        {
            if (client.Controller.Character.DoNotDoTimers)
            {
                return;
            }

            byte moveType = message.MoveType;
            var heading = new Quaternion(message.Heading.X, message.Heading.Y, message.Heading.Z, message.Heading.W);
            Coordinate coordinates = new Coordinate(message.Coordinates);

            int sincePreviousMove = message.MillisecondsSincePreviousMove;
            float tiltWorldZ = message.TiltWorldZ;
            float tiltLocalX = message.TiltLocalX;

            // Keep one authoritative trace at the point where the decoded
            // packet reaches gameplay. This settles whether the graphical
            // client sent movement without requiring another packet capture.
            Log.Info(
                "MOVE_RX character={0} type={1} coordinates={2}/{3}/{4} sincePrevious={5}ms tilt={6}/{7}",
                client.Controller.Character.Identity.Instance,
                moveType,
                coordinates.x,
                coordinates.y,
                coordinates.z,
                sincePreviousMove,
                tiltWorldZ,
                tiltLocalX);

            /*
            if (!client.Character.DoNotDoTimers)
            {
                var teleportPlayfield = WallCollision.WallCollisionCheck(
                    coordinates.x, coordinates.z, client.Character.PlayField);
                if (teleportPlayfield.ZoneToPlayfield >= 1)
                {
                    var coordHeading = WallCollision.GetCoord(
                        teleportPlayfield, coordinates.x, coordinates.z, coordinates);
                    if (teleportPlayfield.Flags != 1337 && client.Character.PlayField != 152
                        || Math.Abs(client.Character.Coordinates.y - teleportPlayfield.Y) <= 2
                        || teleportPlayfield.Flags == 1337
                        && Math.Abs(client.Character.Coordinates.y - teleportPlayfield.Y) <= 6)
                    {
                        client.Teleport(
                            coordHeading.Coordinates, coordHeading.Heading, teleportPlayfield.ZoneToPlayfield);
                        Program.zoneServer.Clients.Remove(client);
                    }

                    return;
                }

                if (client.Character.Stats.LastConcretePlayfieldInstance.Value != 0)
                {
                    var correspondingDoor = DoorHandler.DoorinRange(
                        client.Character.PlayField, client.Character.Coordinates, 1.0f);
                    if (correspondingDoor != null)
                    {
                        correspondingDoor = DoorHandler.FindCorrespondingDoor(correspondingDoor, client.Character);
                        client.Character.Stats.LastConcretePlayfieldInstance.Value = 0;
                        var aoc = correspondingDoor.Coordinates;
                        aoc.x += correspondingDoor.hX * 3;
                        aoc.y += correspondingDoor.hY * 3;
                        aoc.z += correspondingDoor.hZ * 3;
                        client.Teleport(aoc, client.Character.Heading, correspondingDoor.playfield);
                        Program.zoneServer.Clients.Remove(client);
                        return;
                    }
                }
            }
            */

            client.Controller.Move(moveType, coordinates, heading);

            // Some objectives only ask you to go somewhere - see
            // QuestObjectiveType.Reach.
            QuestManager.OnMoved(client.Controller.Character, client.Controller.Character.Coordinates());

            /* Start NV Heading Testing Code
             * Yaw: 0 to 360 Degrees (North turning clockwise to a complete revolution)
             * Roll: Not sure, but is always 0 cause we can't roll in AO
             * Pitch: 90 to -90 Degrees (90 is nose in the air, 0 is level, -90 is nose to the ground)
             */
            /* Comment this line with a '//' to enable heading testing
            client.SendChatText("Raw Headings: X: " + client.Character.heading.x + " Y: " + client.Character.heading.y + " Z:" + client.Character.heading.z);
            
            client.SendChatText("Yaw:  " + Math.Round(180 * client.Character.heading.yaw / Math.PI) + " Degrees");
            client.SendChatText("Roll: " + Math.Round(180 * client.Character.heading.roll / Math.PI) + " Degrees");
            client.SendChatText("Pitch:   " + Math.Round(180 * client.Character.heading.pitch / Math.PI) + " Degrees");
            /* End NV Heading testing code */

            /* start of packet */
            var reply = new CharDCMoveMessage
                        {
                            Identity = client.Controller.Character.Identity,
                            Unknown = 0x00,
                            MoveType = moveType,
                            Heading =
                                new SmokeLounge.AOtomation.Messaging.GameData.Quaternion
                                {
                                    X =
                                        heading
                                        .xf,
                                    Y =
                                        heading
                                        .yf,
                                    Z =
                                        heading
                                        .zf,
                                    W =
                                        heading
                                        .wf
                                },
                            Coordinates =
                                new Vector3
                                {
                                    X = coordinates.x,
                                    Y = coordinates.y,
                                    Z = coordinates.z
                                },
                            MillisecondsSincePreviousMove = sincePreviousMove,
                            TiltWorldZ = tiltWorldZ,
                            TiltLocalX = tiltLocalX
                        };
            // The client already owns this movement: it is the packet we just
            // accepted. Sending the reconstructed packet straight back to the
            // same client is not required for prediction and is the last packet
            // the crash captures show the server producing after the first move
            // key. Tell only the other clients that know this character.
            client.Controller.Character.Playfield.Publish(
                new IMSendAOtomationMessageToPlayfieldOthers
                    {
                        Body = reply,
                        Identity = client.Controller.Character.Identity
                    });

            // TODO: rewrite statelscheck - will move to heartbeat timer too - Algorithman
            /*
            if (Statels.StatelppfonEnter.ContainsKey(client.Character.PlayField))
            {
                foreach (var s in Statels.StatelppfonEnter[client.Character.PlayField])
                {
                    if (s.onEnter(client))
                    {
                        return;
                    }

                    if (s.onTargetinVicinity(client))
                    {
                        return;
                    }
                }
            }
             */
        }

        #endregion
    }
}
