// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgClientMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgClientMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.OrgClient)]
    public class OrgClientMessage : N3Message
    {
        #region Constructors and Destructors

        public OrgClientMessage()
        {
            this.N3MessageType = N3MessageType.OrgClient;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        [AoFlags("flags")]
        public OrgClientCommand Command { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>
        /// Where the command was typed, so the answer can go back there.
        /// </summary>
        /// <remarks>
        /// Nothing in the client computes this. It is the first argument of
        /// N3Msg_TextCommand(int, const char*, const Identity&amp;), handed
        /// straight to the message: the constructor at Gamecode.dll 0x101266B4
        /// stores its second parameter here and nothing else ever writes the
        /// field.
        ///
        /// GUI.dll supplies it, and always from the same place - the chat
        /// window object's own member at +0x1EC, at all three of its call
        /// sites. The client's debug commands take the same leading int and
        /// hand it to the chat text listeners as the first argument of the
        /// dispatch at 0x10012B6E, which is how output finds its way back to
        /// the window that asked for it.
        ///
        /// Commands that come from a dialog rather than a window pass zero.
        /// N3Msg_OrgPromotionConfirmed is the one to look at: it builds this
        /// message directly, with no window to name.
        /// </remarks>
        [AoMember(2)]
        public int Window { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        [AoUsesFlags("flags", typeof(string), FlagsCriteria.EqualsToAny, 
            new[]
                {
                    (int)OrgClientCommand.Create, (int)OrgClientCommand.StartVote, (int)OrgClientCommand.Vote, 
                    (int)OrgClientCommand.Kick, (int)OrgClientCommand.Tax, (int)OrgClientCommand.BankAdd, 
                    (int)OrgClientCommand.BankRemove, (int)OrgClientCommand.History, 
                    (int)OrgClientCommand.Objective, (int)OrgClientCommand.Description, (int)OrgClientCommand.Name, 
                    (int)OrgClientCommand.GoverningForm, (int)OrgClientCommand.StopVote
                })]
        public string CommandArgs { get; set; }

        /// <summary>
        /// Promote only: whether this is the confirmation or the request.
        /// </summary>
        /// <remarks>
        /// The one command that carries neither a string nor nothing. The
        /// writer's jump table at Gamecode.dll 0x101265B4 has three arms - a
        /// string for thirteen commands, nothing for fourteen, and this byte
        /// for Promote alone - and the reader's table at 0x10126698 is the same
        /// table again.
        ///
        /// Which of the two a copy is comes from where it was built. Typing
        /// "/org promote" reaches the constructor at 0x10041093 with zero here;
        /// the client's N3Msg_OrgPromotionConfirmed export, which is what the
        /// dialog calls when a promotion is accepted, reaches it at 0x1001DD8C
        /// with one. The reader normalizes anything non-zero to 1.
        /// </remarks>
        [AoMember(4)]
        [AoUsesFlags("flags", typeof(byte), FlagsCriteria.EqualsToAny, 
            new[] { (int)OrgClientCommand.Promote })]
        public byte Confirmed { get; set; }

        #endregion
    }
}