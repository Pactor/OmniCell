// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientRequestDemolishMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ClientRequestDemolishMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Tear down the building standing on one tile.
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
    /// city.dll exports the other end of it:
    /// CityAI_c::ClientRequestDemolish(Identity const&amp;, TilePos_c const&amp;)
    /// at 0x10002A40 - the requester and the tile, which is exactly what is
    /// here once the city id has said which CityAI_c.
    ///
    /// Reader 0x101302E1, writer 0x10130312, constructor 0x10130341,
    /// dispatcher 0x10130338; vtable 0x101721F4. Class name
    /// ClientRequestDemolishIIR_c.
    /// </remarks>
    [AoContract((int)N3MessageType.ClientRequestDemolish)]
    public class ClientRequestDemolishMessage : N3Message
    {
        #region Constructors and Destructors

        public ClientRequestDemolishMessage()
        {
            this.N3MessageType = N3MessageType.ClientRequestDemolish;
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

        /// <summary>
        /// The tile the building stands on.
        /// </summary>
        /// <remarks>
        /// Read at 0x101302FC through city.dll's TilePos_c operator. The tile
        /// is how a building is named throughout: the server announces a
        /// demolition as SlotPlayfieldHouseDemolishStarted(TilePos_c const&amp;)
        /// and the client looks a house up with GetTemplateAtTilePos.
        /// </remarks>
        [AoMember(1)]
        public TilePos Tile { get; set; }

        #endregion
    }
}
