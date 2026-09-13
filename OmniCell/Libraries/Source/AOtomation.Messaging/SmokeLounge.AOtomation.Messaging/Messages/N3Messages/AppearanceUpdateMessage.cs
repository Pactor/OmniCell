// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppearanceUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AppearanceUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.AppearanceUpdate)]
    public class AppearanceUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public AppearanceUpdateMessage()
        {
            this.N3MessageType = N3MessageType.AppearanceUpdate;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public Texture[] Textures { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public Mesh[] Meshes { get; set; }

        /// <summary>
        /// Stat 673, visualflags.
        /// </summary>
        /// <remarks>
        /// The dispatcher at Gamecode.dll 0x10071C07 writes it straight into
        /// stat 0x2A1. Bit 5 of it is the one the dispatcher then acts on
        /// itself: it looks up inventory slots 61 and 63 - the two weapon hands
        /// - and shows or hides what is in them on the inverse of that bit, so
        /// bit 5 set means the weapons are put away.
        /// </remarks>
        [AoMember(2)]
        public short VisualFlags { get; set; }

        /// <summary>
        /// Whether the character's title is shown. 1 in 2 of the 54 captured
        /// copies.
        /// </summary>
        /// <remarks>
        /// The same field SimpleCharFullUpdate carries under this name, and the
        /// two are the same byte in the client: both land on dynel + 0x208
        /// through the setter at 0x10057800, which also raises the redraw flag
        /// at + 0x2C4 when the value actually changes. The other caller of that
        /// setter, at 0x100785B1, takes it from + 0x32E of a character record
        /// immediately after taking stat 673 from + 0x32C - the same two
        /// fields, in the same order, two bytes apart, exactly as they sit
        /// here.
        /// </remarks>
        [AoMember(3)]
        public byte VisibleTitle { get; set; }

        #endregion
    }
}