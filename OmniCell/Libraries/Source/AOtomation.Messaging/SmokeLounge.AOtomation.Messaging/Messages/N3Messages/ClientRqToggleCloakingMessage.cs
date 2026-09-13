// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientRqToggleCloakingMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ClientRqToggleCloakingMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Turn the city's cloaking device on or off.
    /// </summary>
    /// <remarks>
    /// One of the five messages the city GUI sends, all built the same way. The
    /// sender takes the city's CityAI_c, calls GetID - exported from city.dll at
    /// 0x100027E2, returning a CityID_t - and pushes that structure's +4 into
    /// the message; CityID_t opens with an Identity, read by city.dll's own
    /// identity operator at 0x100274BB, so +4 is its instance half. Nothing else
    /// of the city id goes on the wire, because the server has the rest.
    ///
    /// The client is the only source for this message: it is sent, never
    /// received - the dispatcher does nothing but clear the pass-on flag - and
    /// no capture contains one, because a capture of a city would need a city.
    /// </remarks>
    /// <remarks>
    /// No on-or-off is carried, because there is none to carry: city.dll's
    /// CityAI_c::ClientRequestToggleCloakingDevice(Identity const&amp;) at
    /// 0x10002A6B takes the requester alone and flips whatever state the city
    /// is in. The client asks the same question of itself before offering the
    /// button - IsCloakingTogglePossible at 0x1000A27F, HasCloakingDevice at
    /// 0x1000A121 - and the answer is not repeated on the wire.
    ///
    /// Reader 0x101303D3, writer 0x101303F3, constructor 0x1013040F,
    /// dispatcher 0x10130406; vtable 0x10172238. Class name
    /// ClientRqToggleCloakingIIR_c.
    /// </remarks>
    [AoContract((int)N3MessageType.ClientRqToggleCloaking)]
    public class ClientRqToggleCloakingMessage : N3Message
    {
        #region Constructors and Destructors

        public ClientRqToggleCloakingMessage()
        {
            this.N3MessageType = N3MessageType.ClientRqToggleCloaking;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which city, by the instance half of its CityID_t.
        /// </summary>
        /// <remarks>
        /// The only field. See the class remarks for where it comes from.
        /// </remarks>
        [AoMember(0)]
        public int CityInstance { get; set; }

        #endregion
    }
}
