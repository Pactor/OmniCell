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

    [AoContract((int)N3MessageType.SetPos)]
    public class SetPosMessage : N3Message

    {
        public SetPosMessage()
        {
            this.N3MessageType = N3MessageType.SetPos;
        }

        [AoMember(1)]
        public Vector3 Coordinates { get; set; }

        [AoMember(2)]
        /// <summary>
        /// Whether to move the dynel's last allowed position too. 1 in every
        /// captured copy.
        /// </summary>
        /// <remarks>
        /// A boolean carried as a byte. The dispatcher at 0x100773A3 calls
        /// n3Dynel_t::UpdateLastAllowedPosition with the corrected coordinates
        /// when it is set, which is what stops the client treating the
        /// correction as a rubber-band and walking back.
        /// </remarks>
        public byte UpdateLastAllowedPosition { get; set; }

        [AoMember(3)]
        /// <summary>
        /// A crowd-limiting figure to show the player, or zero for none. Zero
        /// in every captured copy.
        /// </summary>
        /// <remarks>
        /// When it is non-zero and the correction is for the client's own
        /// character, the dispatcher formats it into the Feedback_CrowdLimiting
        /// string. Nothing else reads it.
        /// </remarks>
        public int CrowdLimitingFeedback { get; set; }

        [AoMember(4)]
        /// <summary>
        /// Whether the character should stop moving once corrected. Zero in
        /// every captured copy.
        /// </summary>
        /// <remarks>
        /// A boolean carried as a byte. When set the dispatcher invokes the
        /// same movement-controller stop that StopMovingCmd_t uses, which is
        /// what identifies it - the two reach one function from different
        /// messages.
        /// </remarks>
        public byte StopMoving { get; set; }
    }
}