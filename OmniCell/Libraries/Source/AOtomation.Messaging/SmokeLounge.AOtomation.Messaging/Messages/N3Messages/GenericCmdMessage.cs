// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GenericCmdMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GenericCmdMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.GenericCmd)]
    public class GenericCmdMessage : N3Message
    {
        #region Constructors and Destructors

        public GenericCmdMessage()
        {
            this.N3MessageType = N3MessageType.GenericCmd;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The Verification_e the client keeps on every command.
        /// </summary>
        /// <remarks>
        /// n3Command_t::GetVerification at N3.dll 0x10003727 is one
        /// instruction - it returns this field - so the name is the client''s
        /// own, not a reading of it. The constructor starts it at zero and the
        /// captures carry 0, 1 and 2 and nothing else.
        ///
        /// It belongs to n3Command_t rather than to GenericCmd: the base
        /// reader at N3.dll 0x1000378F reads this and Serial, and only then
        /// does GenericCmd read its own Action.
        /// </remarks>
        [AoMember(0)]
        public int Verification { get; set; }

        /// <summary>
        /// A serial number, so the client can tell its own commands apart.
        /// </summary>
        /// <remarks>
        /// The constructor at N3.dll 0x100038C3 takes this from a global
        /// counter, increments that counter, and adds the value to a list.
        /// n3Command_t::IsForeign at 0x100037E8 walks that same list and
        /// returns true when the value is not in it - so a command is foreign
        /// exactly when this client did not issue it.
        ///
        /// It was called Count, which it is not: a count of what was never
        /// said, and nothing in the message is that many.
        /// </remarks>
        [AoMember(1)]
        public int Serial { get; set; }

        [AoMember(2)]
        public GenericCmdAction Action { get; set; }

        /// <summary>
        /// An int32 on the wire that the client keeps as a boolean.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x1003AC95 compares it against zero with setne and
        /// stores the one byte result, so anything non-zero is the same as one.
        /// The captures only ever carry 0 or 1 anyway - 1,861 and 246 of 2,107.
        ///
        /// What it turns on is not established. It is written back as a whole
        /// int32 because that is what is on the wire.
        /// </remarks>
        [AoMember(3)]
        public int Flag { get; set; }

        [AoMember(4)]
        public Identity User { get; set; }

        /// <summary>
        /// What the action is being done to.
        /// </summary>
        /// <remarks>
        /// How many identities follow is decided by Action, exactly. Across
        /// 2,107 captured copies: all 2,068 that carry Use have one, and the 39
        /// that have two are precisely the 30 UseItemOnItem plus the 9
        /// UseItemOnCharacter. Nothing else in the message says how many there
        /// are, so a reader that ignores Action cannot know where this ends.
        ///
        /// The identity types seen are Inventory, Corpse, Terminal and
        /// VendingMachine, which is the range of things a Use can be aimed at.
        /// </remarks>
        [AoMember(5)]
        public Identity[] Target { get; set; }

        #endregion
    }
}