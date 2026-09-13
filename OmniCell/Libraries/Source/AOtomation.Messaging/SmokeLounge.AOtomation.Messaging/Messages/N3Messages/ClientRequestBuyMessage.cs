// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientRequestBuyMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ClientRequestBuyMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Buy the city plot the player is standing on.
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
    /// What it asks for is settled on the other side: city.dll exports
    /// CityAI_c::ClientRequestBuy(Identity const&amp;) at 0x100029E4, which takes
    /// the requesting character and nothing else - the city is already known,
    /// because the request reached that city's own CityAI_c.
    ///
    /// Reader 0x1013015A, writer 0x1013017A, constructor 0x10130196,
    /// dispatcher 0x1013018D; vtable 0x10172170. The constructor registers the
    /// class name ClientRequestBuyIIR_c, which is what the id hashes from.
    /// </remarks>
    [AoContract((int)N3MessageType.ClientRequestBuy)]
    public class ClientRequestBuyMessage : N3Message
    {
        #region Constructors and Destructors

        public ClientRequestBuyMessage()
        {
            this.N3MessageType = N3MessageType.ClientRequestBuy;
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
