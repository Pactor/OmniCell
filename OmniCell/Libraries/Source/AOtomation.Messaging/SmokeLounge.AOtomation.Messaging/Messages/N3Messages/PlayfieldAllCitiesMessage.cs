// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAllCitiesMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAllCitiesMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Sent on entering a playfield, listing the player cities in it.
    /// </summary>
    /// <remarks>
    /// N3MessageType.PlayfieldAllCities was in the enum with no class behind
    /// it, so these never deserialised.
    ///
    /// All 10 captured were identical: the playfield identity, then a two byte
    /// body of zero. Every capture so far is from the newbie island and the
    /// subway, neither of which has player cities, so an empty message is what
    /// would be expected.
    ///
    /// Two bytes is the point worth noting. The sibling PlayfieldAllTowers
    /// message carries an X3F1 array, whose empty form is the four bytes
    /// 000003F1 - and one capture holds a 5053 byte PlayfieldAllTowers, so that
    /// encoding is confirmed for a populated list. This message is two bytes,
    /// so it is not carrying an X3F1 array and is read as a plain short.
    ///
    /// If a playfield that does contain cities is ever captured and this fails
    /// to deserialise, the short is where to look: it would then be a count in
    /// some other encoding, followed by entries whose shape no capture has yet
    /// shown.
    /// </remarks>
    [AoContract((int)N3MessageType.PlayfieldAllCities)]
    public class PlayfieldAllCitiesMessage : N3Message
    {
        #region Constructors and Destructors

        public PlayfieldAllCitiesMessage()
        {
            this.N3MessageType = N3MessageType.PlayfieldAllCities;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 0 in every captured sample, all of which were from playfields with
        /// no player cities.
        /// </summary>
        /// <summary>
        /// The houses standing in this playfield, or null when there are none.
        /// </summary>
        /// <remarks>
        /// On the wire this is a two-byte count of payload BYTES, not of
        /// houses, followed by that many bytes. Zero means nothing follows at
        /// all - not a list of no houses, but no list: the uint32 house count
        /// inside the payload is absent too. All twelve captured copies are
        /// that case, which is why this was a lone unnamed short for so long.
        ///
        /// The message reader never looks inside the payload. It copies the
        /// bytes into a second stream and hands them to
        /// PlayfieldCityHolderClient_c::UpdateNewHouses, which is where the
        /// shape comes from.
        /// </remarks>
        [AoMember(0)]
        public CityHouse[] Houses { get; set; }

        #endregion
    }
}
