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

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// One change to one district's suppression field.
    /// </summary>
    /// <remarks>
    /// The client calls it FightModeChange_t, in its own words: "Invalid
    /// FightModeChange_t stream" is what its reader prints when it refuses one.
    /// </remarks>
    public class FightModeUpdateEntry
    {
        /// <summary>
        /// The change's own id, so it can be taken out again later.
        /// </summary>
        /// <remarks>
        /// A change carrying <see cref="FightModeChangeFlags.ById"/> is matched
        /// against the district's existing changes on this value - 0x1011FE65
        /// walks the list comparing it - rather than being added by district
        /// name. 88, 89, 124 and 125 across the captured copies.
        /// </remarks>
        [AoMember(1)]
        public int Id { get; set; }

        /// <summary>
        /// Which district. Plago and Harstad in the captured copies.
        /// </summary>
        /// <remarks>
        /// A name and not a number: the dispatcher hands it straight to
        /// GameData's PlayfieldDistrictInfo_t::GetDistrictData, the overload
        /// that takes a string.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string District { get; set; }

        /// <summary>
        /// Whether the change sets or adds, whether it overrides, and whether
        /// it is putting a change in or taking one out.
        /// </summary>
        [AoMember(3)]
        public FightModeChangeFlags Flags { get; set; }

        /// <summary>
        /// How much suppression, or how much to add to it.
        /// </summary>
        /// <remarks>
        /// Signed, and one byte wide. A change that sets carries 0 to 4; one
        /// that adds may carry -4 to 4, and the reader enforces both. 1 and 2 -
        /// seventy five and twenty five percent - in the captured copies.
        /// </remarks>
        [AoMember(4)]
        public SuppressionLevel Level { get; set; }
    }
}