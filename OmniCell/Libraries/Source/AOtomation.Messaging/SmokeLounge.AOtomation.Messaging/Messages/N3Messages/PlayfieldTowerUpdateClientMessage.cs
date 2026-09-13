// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldTowerUpdateClientMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldTowerUpdateClientMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// One tower appearing or disappearing, after
    /// <see cref="PlayfieldAllTowersMessage"/> has sent the whole field.
    /// </summary>
    /// <remarks>
    /// Extracted-client PlayfieldTowerUpdateClientIIR_t has vtable 0x10171964,
    /// reader 0x1012B38A, writer 0x1012B3CC and dispatcher 0x1012B350, and the
    /// three of them together account for every byte.
    ///
    /// The reader takes an Identity and an int32, and reads a tower record
    /// after them only when that int32 is exactly 2 - with 0x1012BB14, the same
    /// reader PlayfieldAllTowers uses for each of its towers, so the record is
    /// a TowerProxy_t and nothing new. The writer emits the same three under
    /// the same condition.
    ///
    /// The dispatcher is what names the int32, and it was traced on 2026-09-11.
    /// It reaches the tower manager - the singleton at 0x102EAB98, returned by
    /// 0x10129A48 - and branches once: on 1 it calls 0x1012A153, which walks
    /// the manager's list comparing each entry's identity against this
    /// message's, hides the matched tower's mesh through
    /// VisualCATMesh_t::DisableVisibility and then erases the entry with the
    /// std::list unlink at 0x1012A923; on anything else it calls 0x1012A0E9,
    /// which copies the record onto the list with the std::list insert at
    /// 0x1012A8A6. Both bump the manager's revision counter at +0x20 and both
    /// re-run the marker geometry the field draws over its towers. So the int32
    /// is an add-or-remove, and <see cref="TowerUpdateAction"/> is those two
    /// values and no others.
    ///
    /// No capture contains one, so
    /// PlayfieldTowerUpdateClientMatchesTheLayoutTakenFromTheClient pins the
    /// layout rather than checking it against retail.
    /// </remarks>
    [AoContract((int)N3MessageType.PlayfieldTowerUpdateClient)]
    public class PlayfieldTowerUpdateClientMessage : N3Message
    {
        public PlayfieldTowerUpdateClientMessage()
        {
            this.N3MessageType = N3MessageType.PlayfieldTowerUpdateClient;
        }

        /// <summary>
        /// Which tower.
        /// </summary>
        /// <remarks>
        /// Read into the message's +0x18 by 0x1013D2F9, the shared Identity
        /// reader. It is what the remove branch matches on, and the add branch
        /// ignores it in favour of the identity inside the record.
        /// </remarks>
        [AoMember(0)]
        public Identity Tower { get; set; }

        /// <summary>
        /// Whether the tower is going in or coming out.
        /// </summary>
        [AoMember(1)]
        [AoFlags("action")]
        public TowerUpdateAction Action { get; set; }

        /// <summary>
        /// The whole tower, on an <see cref="TowerUpdateAction.Add"/>.
        /// </summary>
        /// <remarks>
        /// Absent on a remove: the reader does not consume it and the writer
        /// does not emit it.
        /// </remarks>
        [AoMember(2)]
        [AoUsesFlags("action", typeof(TowerProxyBase), FlagsCriteria.EqualsToAny,
            new[] { (int)TowerUpdateAction.Add })]
        public TowerProxyBase TowerProxy { get; set; }
    }
}
