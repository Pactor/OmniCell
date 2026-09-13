// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharDCMoveMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharDCMoveMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.CharDCMove)]
    public class CharDCMoveMessage : N3Message
    {
        #region Constructors and Destructors

        public CharDCMoveMessage()
        {
            this.N3MessageType = N3MessageType.CharDCMove;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public byte MoveType { get; set; }

        [AoMember(1)]
        public Quaternion Heading { get; set; }

        [AoMember(2)]
        public Vector3 Coordinates { get; set; }

        /// <summary>
        /// Milliseconds since the client last sent a movement message.
        /// </summary>
        /// <remarks>
        /// Measured, not guessed. Across 2,665 client movement packets in one
        /// captured session, this value matched the wall-clock gap since the
        /// previous movement packet to within sixty milliseconds in 97% of
        /// cases; the rest are where another message arrived in between and
        /// moved the reference. It is zero in a little over half of all copies,
        /// which is the client saying nothing has elapsed worth reporting.
        ///
        /// It is why a stop carries a smaller number than a start: a stop
        /// follows its own start within a few hundred milliseconds, and a start
        /// follows however long the player stood still.
        /// </remarks>
        [AoMember(3)]
        public int MillisecondsSincePreviousMove { get; set; }

        /// <summary>
        /// Radians about the world's Z axis, turned onto
        /// <see cref="Heading"/> before it reaches the character.
        /// </summary>
        /// <remarks>
        /// Called a reserved int32 until 2026-09-11 on the strength of being
        /// zero in three thousand captured copies - which says what the retail
        /// server sends, not what the field is.
        ///
        /// It is a float, and the client insists on it: the reader at
        /// 0x1006BEB8 takes this and <see cref="TiltLocalX"/> with the float
        /// operator at 0x101540CC and puts each through MSVCR100 _finite,
        /// failing the whole message if either is not a finite float. Four
        /// bytes that happened to be a NaN bit pattern would be thrown out, so
        /// they cannot be integers.
        ///
        /// What they are for is in the dispatcher. For move types 9 to 14 it
        /// calls n3Dynel_t::VehicleForwardUpdate(const Vector3&amp;, const
        /// Quaternion&amp;, float, float) at 0x1006BE3D, passing the dynel's
        /// current relative position, this message's Heading, and these two
        /// floats. That function - 0x10004EBD in N3.dll - treats both as
        /// angles: it builds a quaternion from the axis (0, 0, 1) and this
        /// one at 0x10004F25, builds a second from
        /// <see cref="TiltLocalX"/> about the axis (1, 0, 0) rotated by
        /// Heading, multiplies both onto Heading, normalises, and hands the
        /// result to the character's Vehicle_t through
        /// Vehicle_t::SetRelRot or SetRelPosRot.
        ///
        /// So they are two extra rotations composed onto the sent heading.
        /// With both zero the composition is the heading itself, which is why
        /// three thousand copies of zero look like a field that does nothing.
        /// </remarks>
        [AoMember(4)]
        public float TiltWorldZ { get; set; }

        /// <summary>
        /// Radians about the (1, 0, 0) axis rotated by <see cref="Heading"/>.
        /// See <see cref="TiltWorldZ"/>, which is read and used the same way.
        /// </summary>
        [AoMember(5)]
        public float TiltLocalX { get; set; }

        #endregion
    }
}