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

    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// The battlestation scoreboard.
    /// </summary>
    /// <remarks>
    /// Two arrays and nothing else, which is settled by disassembling the
    /// client's own reader rather than inferred.
    ///
    /// SendScoreIIR_t's reader at Gamecode 0x10132050 does exactly four things:
    /// it takes the stream, calls the helper at 0x101321D1 with the member at
    /// the message + 0x18, adds 0x28 and calls the same helper again with the
    /// member there, and returns whether the stream faulted. Two members,
    /// sixteen bytes apart, read by one function.
    ///
    /// That helper is what makes them arrays of int32. It reads a word, divides
    /// it by 0x3F1 and refuses the message unless the remainder is zero - the
    /// X3F1 count encoding, where the count is the quotient less one. It then
    /// refuses a count above 0x7530, thirty thousand. Then it loops that many
    /// times, reading one word each pass through the same stream reader and
    /// pushing it into the vector at 0x100458BB. One int32 per entry, nothing
    /// nested.
    ///
    /// This is worth writing down because a second model of this message
    /// disagrees. AOSharp, in AOBuddy10, reads it flat: an integer, four
    /// battlestation sides, another integer, then a red score and a blue
    /// score. There is no arrangement of the reader above that produces that,
    /// and no captured copy of this message exists here to appeal to - so the
    /// disassembly decides it, and the flat model is wrong. Its field names
    /// were attached to the wrong bytes and are not evidence of anything,
    /// however plausible Red/Blue/None sounds for a three-state site.
    /// </remarks>
    [AoContract((int)N3MessageType.SendScore)]
    public class SendScoreMessage : N3Message
    {
        #region Constructors and Destructors

        public SendScoreMessage()
        {
            this.N3MessageType = N3MessageType.SendScore;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The scoreboard's numbers. Element 0 is the Clan total and element 1
        /// the Omni total; anything after them is read by nobody.
        /// </summary>
        /// <remarks>
        /// The first two are named by what the window does with them, not by
        /// their position: the handler in GUI.dll wraps element 0 in a centred
        /// div and hands it to the view called "ClanVP", and does the same with
        /// element 1 into "OmniVP". Both go through the ostream operator for
        /// int, so they are counts rather than ids.
        ///
        /// Neither read is bounded by the array's length, so the server always
        /// sends at least two. Whether it ever sends more than two, and what
        /// those would mean, is not answerable from the client - nothing reads
        /// past element 1.
        /// </remarks>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public int[] Scores { get; set; }

        /// <summary>
        /// One value per battlestation site, each a state in a set of three.
        /// </summary>
        /// <remarks>
        /// The handler checks this array's length against a list the window
        /// already holds and gives up when they differ, then walks the two
        /// together and hands entry i's value to row i. The row uses it as an
        /// index into the window's three-texture vector, which is loaded at
        /// GUI.dll 0x100F8AE4 as texture 301, then 303, then 302 - so index 0
        /// is texture 301, index 1 is 303 and index 2 is 302.
        ///
        /// Which of the three means what is still open. Naming them needs the
        /// texture ids resolved, and the identifier the client passes to
        /// GetGuiTexture is not the line index of Graphics.uvgi - 301 to 303
        /// there are an experience bar and two CVP widgets, which is not a set
        /// of three site states. The obvious guess is Clan, Omni and neutral,
        /// and there are MAPSQUARE_CLAN, MAPSQUARE_OMNI and MAPSQUARE_NEUTRAL
        /// textures in that file to guess with, but a guess is what it would
        /// be.
        /// </remarks>
        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public int[] SiteStates { get; set; }

        #endregion
    }
}
