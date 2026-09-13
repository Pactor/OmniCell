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

    [AoContract((int)N3MessageType.PetToMaster)]
    public class PetToMasterMessage : N3Message
    {
        public PetToMasterMessage()
        {
            this.N3MessageType = N3MessageType.PetToMaster;
        }

        /// <summary>
        /// The pet this is about.
        /// </summary>
        /// <remarks>
        /// The dispatcher at 0x1007699D resolves it first and gives up if it is
        /// not in the playfield.
        /// </remarks>
        [AoMember(1)]
        public Identity PetIdentity { get; set; }

        /// <summary>
        /// Whether <see cref="AttachNotificationValue"/> is carried: 1 sends it, 2 sends nothing,
        /// and the client ignores the message on anything else.
        /// </summary>
        /// <remarks>
        /// The dispatcher decrements this twice to decide. One takes the branch
        /// at 0x100769D2, which pushes the field below; two falls through to
        /// 0x100769CF, where the register is already zero and that zero is
        /// pushed instead; anything else leaves at 0x100769E2 having done
        /// nothing. Both values appear in the captures, each with the value the
        /// branch implies - 1 with a 10, 2 with a 0.
        /// </remarks>
        [AoMember(2)]
        public int Operation { get; set; }

        /// <summary>
        /// The compatibility signal value, when <see cref="Operation"/> is 1.
        /// </summary>
        /// <remarks>
        /// The dispatcher puts it on
        /// the pet signal - GlobalSignals_c's instance plus 0x40, raised at
        /// 0x10011FA1 - and nothing in this client subscribes to that signal.
        /// Gamecode.dll is the only module that touches offset 0x40 of the
        /// signals object, and it only raises it; GUI.dll and Interfaces.dll
        /// both import GlobalSignals_c::GetInstance and neither ever asks for
        /// that offset. Thus it is retained compatibility data with no behavior
        /// in this client. Retail attach packets use 10 and detach packets use 0.
        ///
        /// 10 in every captured attach copy.
        /// </remarks>
        [AoMember(3)]
        public int AttachNotificationValue { get; set; }

        /// <summary>
        /// An identity the client reads and never looks at.
        /// </summary>
        /// <remarks>
        /// The reader takes it into the message's + 0x28 at 0x10076944 and the
        /// dispatcher touches + 0x18, + 0x04, + 0x20 and + 0x24 and nothing
        /// else. Empty in all four captured copies.
        /// </remarks>
        [AoMember(4)]
        public Identity Unread { get; set; }
    }
}
