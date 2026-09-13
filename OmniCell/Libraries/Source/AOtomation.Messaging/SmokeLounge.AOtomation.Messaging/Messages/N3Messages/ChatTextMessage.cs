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

    [AoContract((int)N3MessageType.ChatText)]
    public class ChatTextMessage : N3Message
    {
        #region Constructors and Destructors

        public ChatTextMessage()
        {
            this.N3MessageType = N3MessageType.ChatText;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The line. A two byte count and no terminator.
        /// </summary>
        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        /// <summary>
        /// Which of the client's chat colours to print it in.
        /// </summary>
        /// <remarks>
        /// The dispatcher at Gamecode.dll 0x100388C7 hands this to the engine's
        /// chat text signal as its third argument; GUI.dll subscribes to that
        /// signal at 0x10083E7B, finds the window <see cref="Window"/> names,
        /// and calls its AddLine with the text and this. AddLine looks the
        /// value up in the colour table at 0x10268D38 and wraps the line in
        /// &lt;font color=NAME&gt; - or leaves the line alone when the value is
        /// zero, which is what makes None a real value rather than a missing
        /// one. The captured line is a system announcement and carries 16,
        /// which the table calls CCYellow.
        /// </remarks>
        [AoMember(1)]
        public ChatTextColour Colour { get; set; }

        /// <summary>
        /// Where the line goes: 0 to a chat window, 1 to the screen.
        /// </summary>
        /// <remarks>
        /// The reader at 0x1003896D refuses anything above 1, and the
        /// dispatcher at 0x1003889D is a single branch on it. Zero takes the
        /// chat text signal above. One takes the other arm, which builds a
        /// colour out of three floats the client keeps for it and calls
        /// 0x1001137A - the floating on-screen message, which has no window and
        /// ignores <see cref="Colour"/> entirely.
        /// </remarks>
        [AoMember(2)]
        public byte OnScreen { get; set; }

        /// <summary>
        /// Which chat window to print in.
        /// </summary>
        /// <remarks>
        /// The same routing value OrgClient carries in the other direction, in
        /// the same argument position of the same signal: the GUI subscriber at
        /// 0x10083E8E looks the value up as a window id when it is below
        /// 0x40000000 and treats it by name above that. Zero in the one
        /// captured copy.
        /// </remarks>
        [AoMember(3)]
        public int Window { get; set; }

        #endregion
    }
}