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
    /// The list of places the grid will take you, and the token that has to
    /// come back with the choice.
    /// </summary>
    /// <remarks>
    /// Ported from AOSharp, with one change. AOSharp carries its own
    /// GridDestinationInfo - a playfield id, two unnamed integers, a string,
    /// and two more unnamed integers. That is the same six fields as the
    /// GridDestination this project already had, which names all of them: the
    /// two integers after the playfield id are an Identity, and the two at the
    /// end are the area's level and type.
    ///
    /// Those last two were settled on 2026-09-12 out of the client's own
    /// localisation database, through the organization message that carries
    /// the same record - category 0x1FC key "LC_AreaInfo" reads
    /// "In: %s / Area: %s / Type: %s / Level: %d" and is fed from them. So the
    /// existing type is used rather than a second, blanker copy of it.
    ///
    /// OmniCell had a documentation page for this message and no class at all.
    /// </remarks>
    [AoContract((int)N3MessageType.GridDestinationSelect)]
    public class GridDestinationSelectMessage : N3Message
    {
        #region Constructors and Destructors

        public GridDestinationSelectMessage()
        {
            this.N3MessageType = N3MessageType.GridDestinationSelect;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Where this grid terminal can send you.
        /// </summary>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public GridDestination[] Destinations { get; set; }

        /// <summary>
        /// Handed back with the destination the player picks.
        /// </summary>
        /// <remarks>
        /// A byte and two integers, none of them identified. AOSharp carries
        /// the three and returns them unaltered, which is all a client has to
        /// do with a token; what they mean is not established by that.
        /// </remarks>
        [AoMember(1)]
        public GridInteractionToken Token { get; set; }

        #endregion
    }
}
