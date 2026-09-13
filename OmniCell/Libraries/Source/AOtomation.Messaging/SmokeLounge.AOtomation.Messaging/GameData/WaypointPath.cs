// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WaypointPath.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the WaypointPath type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The path SimpleCharFullUpdate carries behind HasWaypoints.
    /// </summary>
    /// <remarks>
    /// An Identity, then a plain int32 count, then that many Vector3s. The
    /// count is a plain int32 and not the X3F1 form every other array in this
    /// message uses, which is worth knowing before assuming otherwise.
    ///
    /// The client sizes its own array at thirty and stops reading there, zeroing
    /// whatever it did not fill. Nothing here imposes that limit: a longer list
    /// would be a packet the client cannot read, and silently truncating it
    /// would hide that rather than fix it.
    /// </remarks>
    public class WaypointPath
    {
        #region AoMember Properties

        [AoMember(0)]
        public Identity Target { get; set; }

        [AoMember(1)]
        public Vector3[] Points { get; set; }

        #endregion
    }
}
