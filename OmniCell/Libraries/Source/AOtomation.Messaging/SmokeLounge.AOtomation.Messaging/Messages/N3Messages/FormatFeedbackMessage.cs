// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFeedbackMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FormatFeedbackMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.FormatFeedback)]
    public class FormatFeedbackMessage : N3Message
    {
        #region Constructors and Destructors

        public FormatFeedbackMessage()
        {
            this.N3MessageType = N3MessageType.FormatFeedback;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which chat category the text belongs to, or zero for none.
        /// </summary>
        /// <remarks>
        /// The dispatcher at 0x100395BB hands this, the kind below and the
        /// formatted string to 0x1005AFF0, which for kind 0 passes this as the
        /// first argument of the client's text-output call at 0x10012B6E. That
        /// routine walks whatever is subscribed to the output object's + 0x17C
        /// and gives each subscriber the value and the text, so a subscriber
        /// decides what to do with it rather than the caller.
        ///
        /// It is a chat category id, and GUI.dll holds the table. Twenty three
        /// of them are registered there as a name followed by the id, in the
        /// shape `push "Me Cast Nano"; push 0x42000018; call 0x100841C7`:
        ///
        ///   0x42000001 Me hit by environment    0x42000002 Me hit by nano
        ///   0x42000003 Your pet hit by nano     0x42000004 Other hit by nano
        ///   0x42000005 You hit other with nano  0x42000006 Me hit by monster
        ///   0x42000007 Me hit by player         0x42000008 You hit other
        ///   0x42000009 Your pet hit by other    0x4200000A Other hit by other
        ///   0x4200000B Me got XP                0x4200000C Me got SK
        ///   0x42000011 Your pet hit by monster  0x42000012 Your misses
        ///   0x42000013 Other misses             0x42000014 You gave health
        ///   0x42000015 Me got health            0x42000016 Me got nano
        ///   0x42000017 You gave nano            0x42000018 Me Cast Nano
        ///   0x4200001A Team Loot Messages       0x4200001B Vicinity Loot
        ///   0x4200001C Research
        ///
        /// These are the categories the chat window filters on, which is what
        /// makes the field a route: a message sent under one of them lands in
        /// whichever window the player has that category switched on for.
        /// 0x4200000D through 0x42000010 and 0x42000019 are used by the
        /// settings code in GUI.dll and never given a name, so they look
        /// retired.
        ///
        /// Gamecode uses two of them itself, and both are the shape above -
        /// 0x100A23FE pushes 0x4200000B, Me got XP, straight into 0x10012B6E,
        /// and 0x42000018, Me Cast Nano, appears at twenty two sites. Most of
        /// the seventy seven callers pass zero, and so does every one of the
        /// 168 captured copies of this message, so zero is the no-category
        /// route and a value picks one of the above.
        /// </remarks>
        [AoMember(0)]
        public int ChatCategory { get; set; }

        /// <summary>
        /// The formatted-string blob, not plain text: the leading run selects
        /// a template and the rest is its text or its arguments.
        /// </summary>
        [AoMember(1,SerializeSize = ArraySizeType.Int16)]
        public string FormattedMessage { get; set; }

        /// <summary>
        /// Which kind of blob <see cref="FormattedMessage"/> is: 0 for a single
        /// literal string, 1 for a template carrying arguments.
        /// </summary>
        /// <remarks>
        /// 0 in 158 of the 168 captured copies and 1 in the other ten, and
        /// every one of those ten is a tradeskill combine result naming the two
        /// items used and the item produced.
        /// </remarks>
        [AoMember(2)]
        public int PayloadKind { get; set; }

        #endregion
    }
}