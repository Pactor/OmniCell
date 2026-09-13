// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AoTransportSignalMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AoTransportSignalMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// An envelope: a signal number and a body only the subscriber understands.
    /// </summary>
    /// <remarks>
    /// Extracted-client AOTransportSignalIIR_c has vtable 0x10171EF8, reader
    /// 0x1012FAAB and writer 0x1012FA7E, and between them there is nothing left
    /// to find. The reader takes an int32 and then hands the rest of the stream
    /// to a BinaryStream member whole - it asks the stream for its size, takes
    /// the difference from where it has got to, copies that many bytes and
    /// seeks past them - and the writer does the same in reverse, emitting the
    /// int32 and then the body's bytes with no length in front of them. So the
    /// body has no length of its own: it is whatever is left in the packet, and
    /// the reader refuses a packet that leaves nothing.
    ///
    /// This message does not know what is in the body and is not supposed to.
    /// Its dispatcher at 0x1012FBC1 passes the number and the body to
    /// 0x1012FDAA, which looks the number up in a registry, and then, for each
    /// subscriber registered under it, rewinds the body to the start and calls
    /// the subscriber to parse it. Two subscribers under the same number read
    /// the same bytes twice, from the beginning, each in its own way.
    ///
    /// Nothing in the registry is static, so the numbers cannot be listed from
    /// the executable - they are put there at run time by whatever subscribes.
    /// That is a property of the design rather than a gap in the reading: the
    /// packet is complete as it stands, in the way a length-prefixed string is
    /// complete without anyone knowing the text.
    ///
    /// No capture contains one, so the layout is the client's word and not the
    /// wire's. It is a short reader and both halves of it agree.
    /// </remarks>
    [AoContract((int)N3MessageType.AoTransportSignal)]
    public class AoTransportSignalMessage : N3Message
    {
        #region Constructors and Destructors

        public AoTransportSignalMessage()
        {
            this.N3MessageType = N3MessageType.AoTransportSignal;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which signal this is, and so who gets the body.
        /// </summary>
        /// <remarks>
        /// Read at 0x1012FACD and written at 0x1012FA84. The dispatcher looks
        /// it up in the subscriber registry and does nothing at all when it
        /// finds no one.
        /// </remarks>
        [AoMember(0)]
        public int Signal { get; set; }

        /// <summary>
        /// The body, which runs to the end of the packet.
        /// </summary>
        /// <remarks>
        /// No count in front of it: the reader takes the rest of the stream and
        /// the writer emits the bytes bare. The reader treats an empty one as a
        /// bad packet and abandons the message.
        ///
        /// See AoTransportSignalSerializer - a field that is "the rest of it"
        /// is not something the attribute serializer can say.
        /// </remarks>
        [AoMember(1)]
        public byte[] Payload { get; set; }

        #endregion
    }
}
