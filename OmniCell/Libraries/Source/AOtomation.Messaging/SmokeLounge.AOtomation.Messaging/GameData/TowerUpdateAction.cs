// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TowerUpdateAction.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TowerUpdateAction type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// What a <see cref="Messages.N3Messages.PlayfieldTowerUpdateClientMessage"/>
    /// does to the client's list of towers.
    /// </summary>
    /// <remarks>
    /// Both values are the client's, taken from the only two places that look
    /// at the field. The reader at 0x1012B38A reads a tower record after it
    /// only when it is exactly 2; the dispatcher at 0x1012B350 erases when it
    /// is exactly 1 and inserts otherwise.
    ///
    /// There is no third value. Anything that is neither 1 nor 2 reads no
    /// record and then inserts the empty one - which the client will happily
    /// do and nothing in it ever asks for.
    /// </remarks>
    public enum TowerUpdateAction
    {
        /// <summary>
        /// Take the tower with this identity out of the list.
        /// </summary>
        /// <remarks>
        /// The dispatcher's 1 branch walks the manager's list comparing each
        /// entry's identity against the message's, and on a match hides the
        /// tower's mesh through VisualCATMesh_t::DisableVisibility before
        /// unlinking and destroying the entry.
        /// </remarks>
        Remove = 1,

        /// <summary>
        /// Put the tower carried in this message into the list.
        /// </summary>
        /// <remarks>
        /// The dispatcher copies the record onto the manager's list and bumps
        /// its revision counter.
        /// </remarks>
        Add = 2
    }
}
