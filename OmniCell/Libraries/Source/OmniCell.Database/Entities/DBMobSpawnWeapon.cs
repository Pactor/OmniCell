#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
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

namespace OmniCell.Database.Entities
{
    #region Usings ...

    using OmniCell.Database.Dao;

    #endregion

    /// <summary>
    /// A weapon in a spawned character's hands.
    /// </summary>
    /// <remarks>
    /// This is what a WeaponItemFullUpdate needs to say, and no more. The live
    /// server sends one of these straight after the SimpleCharFullUpdate of any
    /// armed character - 2454 of them across the captured sessions - and without
    /// it every NPC in the world stands there empty handed.
    ///
    /// Held apart from mobspawnsinventory, which nothing reads and whose columns
    /// do not line up with this message. A mob carrying a real inventory is a
    /// larger change; this is the display, which is what the client is missing.
    /// </remarks>
    [Tablename("mobspawnsweapons")]
    public class DBMobSpawnWeapon : IDBEntity
    {
        /// <summary>
        /// Surrogate key, filled in by the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The spawn holding it. Named to match the mobspawns column it joins to.
        /// </summary>
        public int SpawnId { get; set; }

        /// <summary>
        /// </summary>
        public int Playfield { get; set; }

        /// <summary>
        /// Identity type of the weapon instance, WeaponInstance in every capture.
        /// </summary>
        public int WeaponType { get; set; }

        /// <summary>
        /// Identity instance of the weapon.
        /// </summary>
        public int WeaponInstance { get; set; }

        /// <summary>
        /// Inventory page byte carried by WeaponItemFullUpdate.
        /// </summary>
        public int? InventoryId { get; set; }

        /// <summary>
        /// Equipped placement carried by WeaponItemFullUpdate.
        /// </summary>
        public int? BodyLocation { get; set; }

        /// <summary>
        /// </summary>
        public int ItemFlags { get; set; }

        /// <summary>
        /// </summary>
        public int ItemLowId { get; set; }

        /// <summary>
        /// </summary>
        public int ItemHighId { get; set; }

        /// <summary>
        /// </summary>
        public int QualityLevel { get; set; }

        /// <summary>
        /// Varies per weapon and is not understood. Carried through as captured
        /// rather than zeroed, because a value the client reads and we do not
        /// is exactly the kind of thing that makes a model render wrongly.
        /// </summary>
        public int Unknown6 { get; set; }

        /// <summary>
        /// As Unknown6. Zero in most captures.
        /// </summary>
        public int Unknown7 { get; set; }

        /// <summary>
        /// Optional ItemDelay stat (294) from the captured weapon payload.
        /// </summary>
        public int? ItemDelay { get; set; }

        /// <summary>
        /// Optional RechargeDelay stat (210) from the captured weapon payload.
        /// </summary>
        public int? RechargeDelay { get; set; }

        /// <summary>
        /// Energy stat (26), including the captured -1 no-update sentinel.
        /// </summary>
        public int? Energy { get; set; }
    }
}
