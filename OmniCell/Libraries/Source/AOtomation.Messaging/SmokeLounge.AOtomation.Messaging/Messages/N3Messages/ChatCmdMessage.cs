// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatCmdMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ChatCmdMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A slash command on its way to the server.
    /// </summary>
    /// <remarks>
    /// The client calls this Fanatic::FanaticIIR_t, in Fanatic.dll - 7,754
    /// bytes of text that hold nothing else - and it only ever sends one: the
    /// dispatcher at 0x10001109 is one call to ClearToBePassedOn and a return.
    /// Vtable 0x100031C0, reader 0x10001214, writer 0x10001112.
    ///
    /// Fanatic.dll exports exactly one function and it is the sender,
    /// Fanatic::ClientInterface_c::Command(int, Identity const&amp;,
    /// std::string const&amp;), whose three parameters are the three fields in
    /// order. GUI.dll is the only module that imports it, and calls it from
    /// four places - 0x100B3737, 0x100B383E, 0x100B3A46 and 0x100B3FD1 - all
    /// inside one run of code that also holds a table of slash commands.
    /// </remarks>
    [AoContract((int)N3MessageType.ChatCmd)]
    public class ChatCmdMessage : N3Message
    {
        #region Constructors and Destructors

        public ChatCmdMessage()
        {
            this.N3MessageType = N3MessageType.ChatCmd;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// An id the four senders take from the object the command is acting
        /// on. Which object is not established.
        /// </summary>
        /// <remarks>
        /// Every one of the four GUI call sites reads it the same way -
        /// <c>mov eax, [ebp + 8]; push [eax + 0x1EC]</c> - so it is the + 0x1EC
        /// of the handler's first argument, and the handler is a slash-command
        /// slot, so that argument is whatever the command dispatcher hands it.
        ///
        /// What the commands are is now known, and it narrows the question
        /// without answering it. The registration block at GUI.dll 0x100B4260
        /// binds the handler at 0x100B36DA to "/get", "/getfull" and "/set",
        /// and the criteria string sitting in the same block is
        /// "stat:gmlevel != 0". These are GM commands, and a GM command that
        /// gets and sets is acting on something the id must name.
        ///
        /// The offset will not name it. A displacement scan finds 91 plain references to
        /// + 0x1EC across GUI.dll, spread over classes that have nothing to do
        /// with each other, so the offset does not identify a type. An earlier
        /// note here suggested joining it to a class that fills + 0x1EC from a
        /// global counter; that does not hold. The three writes to the offset
        /// that exist all write + 0x1EC and + 0x1F0 together, which is a pair -
        /// an Identity or a point - and not a counter.
        /// </remarks>
        [AoMember(0)]
        public int Unknown1 { get; set; }

        /// <summary>
        /// Where the command is aimed.
        /// </summary>
        /// <remarks>
        /// The GUI fills it from the player: 0x1001B714 returns the local
        /// character object and the senders copy its + 0xC0 and + 0xC4.
        ///
        /// The client's own reader has a fallback - if both halves arrive zero
        /// it fills them from n3InfoItem_t::GetDestinationID - which is worth
        /// knowing but is not where the value comes from on the way out.
        /// </remarks>
        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>
        /// The command line, with its leading slash already removed.
        /// </summary>
        /// <remarks>
        /// An int32 length and that many raw bytes, with no terminator. The
        /// reader refuses a length above 0x2710 - ten thousand - or longer than
        /// the stream has left.
        ///
        /// The slash is dropped by the sender, not by us: at GUI.dll 0x100B3707
        /// the caller takes the typed string's buffer, steps one character past
        /// it, and builds the argument from the rest.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.Int32)]
        public string Command { get; set; }

        #endregion
    }
}