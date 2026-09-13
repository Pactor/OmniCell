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
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// What is in the character's own bank.
    /// </summary>
    /// <remarks>
    /// A container and nothing else. The reader at Gamecode 0x10072518 pushes
    /// the container and the stream and calls the shared container reader at
    /// 0x1002A5DB - the same one FullCharacter's inventory and BankCorpse use -
    /// and then stops; the writer at 0x10072537 does the same in reverse. The
    /// int32 and Identity that used to stand after the list here are not on the
    /// wire at all, and were twelve bytes of our own invention.
    ///
    /// The dispatcher at 0x1007254B is what makes this the character's own
    /// bank rather than somebody else's: it resolves the message identity to a
    /// character and writes that character's own instance into the container's
    /// +0x24, where BankCorpse takes an instance off the wire instead.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.Bank)]
    public class BankMessage : N3Message
    {
        #region Constructors and Destructors

        public BankMessage()
        {
            this.N3MessageType = N3MessageType.Bank;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// What is in it.
        /// </summary>
        /// <remarks>
        /// Each entry is the shared container record: a placement, two int16s,
        /// an Identity and a GameData::ACGItem_t.
        /// </remarks>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Contents { get; set; }

        #endregion
    }
}