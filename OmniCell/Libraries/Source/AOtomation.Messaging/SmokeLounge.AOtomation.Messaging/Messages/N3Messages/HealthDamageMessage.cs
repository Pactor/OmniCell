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

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    [AoContract((int)N3MessageType.HealthDamage)]
    public class HealthDamageMessage : N3Message
    {
        public HealthDamageMessage()
        {
            this.N3MessageType = N3MessageType.HealthDamage;
        }

        /// <summary>
        /// Stat 27, health - what the character has left after the change.
        /// </summary>
        /// <remarks>
        /// The dispatcher at 0x100A075E writes it straight into stat 0x1B.
        /// </remarks>
        [AoMember(1)]
        public int Health { get; set; }

        /// <summary>
        /// How much health moved, signed: negative is damage and anything else
        /// is a heal.
        /// </summary>
        /// <remarks>
        /// The dispatcher branches on the sign at 0x100A0664 and then displays
        /// the magnitude - it takes the absolute value with the usual
        /// cdq/xor/sub before handing it to the feedback line - so the sign
        /// chooses the line and the size fills it in.
        /// </remarks>
        [AoMember(2)]
        public int Delta { get; set; }

        /// <summary>
        /// What kind of damage it was.
        /// </summary>
        /// <remarks>
        /// The client's own word for this field: the formatter it ends up in
        /// looks the value up in a damage type table at Gamecode.dll 0x10036CF5
        /// and prints "Missing damagetype: %d" when it is not there. The table
        /// is built in one run at 0x10033C41 and DamageType is the whole of it.
        ///
        /// Zero is not a value in that table; the formatter substitutes 27,
        /// Unknown, at 0x10012C87 before looking anything up. The damage arm
        /// also tests for 474, Fall, before anything else, because falling has
        /// a message of its own.
        ///
        /// The captures only ever carry 0, 92, 95 and 96 - unset, energy, cold
        /// and poison, which is what a metaphysicist's pets and nanos deal.
        /// </remarks>
        [AoMember(3)]
        public DamageType DamageType { get; set; }

        /// <summary>
        /// What killed the character, or None.
        /// </summary>
        /// <remarks>
        /// Non-zero sends the dispatcher into 0x1005B3D8, which switches on it
        /// to pick one of five Feedback_DeathBy... lines and then sets health
        /// to zero. None in all 447 captured copies.
        /// </remarks>
        [AoMember(4)]
        public DeathCause DeathCause { get; set; }

        /// <summary>
        /// Who did it - the attacker, or the healer.
        /// </summary>
        /// <remarks>
        /// Not the target: the message's own Identity is the character whose
        /// health changed, and the dispatcher refuses to do anything unless it
        /// is a CanbeAffected. This one is resolved separately at 0x100A064E
        /// and handed to the feedback line as the other party. For a character
        /// healing itself the two are the same.
        /// </remarks>
        [AoMember(5)]
        public Identity Source { get; set; }

        /// <summary>
        /// The item that did it, as an instance. Zero when there is none, which
        /// is all 447 captured copies.
        /// </summary>
        /// <remarks>
        /// It is half an identity and the client supplies the other half. The
        /// formatter tests it against zero at 0x10012C97 and, when it is set,
        /// builds an Identity with the type hard-coded to 1000020 and this as
        /// the instance, then hands it to the item manager's find-or-create at
        /// 0x10082EE2. What that builds is a DummyItemBase_t, and the formatter
        /// calls vtable slot 0x34 on it - a name getter that falls back to stat
        /// 446, nametemplate, when the object has no name of its own - and puts
        /// the answer in the message.
        ///
        /// So this names the weapon or the nano the damage came from, and the
        /// captures carry none because nothing in them dealt damage through an
        /// item the client had to name.
        /// </remarks>
        [AoMember(6)]
        public int SourceItem { get; set; }
    }
}