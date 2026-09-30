// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TemplateActionMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TemplateActionMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.TemplateAction)]
    public class TemplateActionMessage : N3Message
    {
        #region Constructors and Destructors

        public TemplateActionMessage()
        {
            this.N3MessageType = N3MessageType.TemplateAction;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int ItemLowId { get; set; }

        [AoMember(1)]
        public int ItemHighId { get; set; }

        [AoMember(2)]
        public int Quality { get; set; }

        /// <summary>
        /// The quantity the action is for: 1, or the stack size of an item arriving through the
        /// overflow window (50 for fifty, 20260914-124401 #5493). "Amount" on the protocol page.
        /// </summary>
        [AoMember(3)]
        public int Amount { get; set; }

        /// <summary>
        /// The action (GameData Action_e): 3 use, 6 wear, 7 remove, 32 use on a character, 87 an
        /// item delivered to the overflow window.
        /// </summary>
        [AoMember(4)]
        public int Action { get; set; }

        [AoMember(5)]
        public Identity Placement { get; set; }

        /// <summary>
        /// The second identity of the action, as a type: 50000 (CanbeAffected) when the item is used on
        /// a character, 0 when there is none.
        /// </summary>
        [AoMember(6)]
        public int TargetType { get; set; }

        /// <summary>
        /// The second identity's instance: the character the item is used on (20260914-124401 #4521,
        /// the stim on Wounded Dockworker 2052536078), or 0.
        /// </summary>
        [AoMember(7)]
        public int TargetInstance { get; set; }

        #endregion
    }
}