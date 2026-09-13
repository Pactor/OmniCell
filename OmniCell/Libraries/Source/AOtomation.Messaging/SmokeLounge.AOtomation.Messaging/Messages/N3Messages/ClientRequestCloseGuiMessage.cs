// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientRequestCloseGuiMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ClientRequestCloseGuiMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The player has closed the city window.
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
    /// The counterpart of the session the server opens: city.dll's
    /// CityClientInterface_c::SlotCloseGUISession(CityID_t const&amp;) at
    /// 0x1000A3DB is the end of what SlotOpenGUISession began, and both are
    /// keyed by the city id this message carries.
    ///
    /// Reader 0x1013021A, writer 0x1013023A, constructor 0x10130256,
    /// dispatcher 0x1013024D; vtable 0x101721B0. Class name
    /// ClientRequestCloseGUIIIR_c.
    /// </remarks>
    [AoContract((int)N3MessageType.ClientRequestCloseGui)]
    public class ClientRequestCloseGuiMessage : N3Message
    {
        #region Constructors and Destructors

        public ClientRequestCloseGuiMessage()
        {
            this.N3MessageType = N3MessageType.ClientRequestCloseGui;
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
