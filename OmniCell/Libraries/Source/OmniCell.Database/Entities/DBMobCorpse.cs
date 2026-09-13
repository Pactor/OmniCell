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
    /// What is left behind when one kind of creature dies.
    /// </summary>
    /// <remarks>
    /// Read out of the 154 corpses in the captured sessions. Everything in a
    /// CorpseFullUpdate that differs between one kind of creature and another is
    /// here; everything constant across all 154 is in the message handler.
    ///
    /// CatMesh is the corpse model and DeadTimer is how long it lies there -
    /// sixty seconds in every capture. Cash is what is on it.
    /// </remarks>
    [Tablename("mobcorpses")]
    public class DBMobCorpse : IDBEntity
    {
        /// <summary>
        /// Surrogate key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The creature's name, not the corpse's - "Cleaning Robot", not
        /// "Remains of Cleaning Robot".
        /// </summary>
        public string MobName { get; set; }

        /// <summary>
        /// Stat 42, the corpse model.
        /// </summary>
        public int CatMesh { get; set; }

        /// <summary>
        /// Stat 8.
        /// </summary>
        public int TimeExist { get; set; }

        /// <summary>
        /// Stat 61, what can be looted off it.
        /// </summary>
        public int Cash { get; set; }

        /// <summary>
        /// Stat 223.
        /// </summary>
        public int CanChangeClothes { get; set; }

        /// <summary>
        /// Stat 360.
        /// </summary>
        public int MonsterScale { get; set; }

        /// <summary>
        /// Stat 4.
        /// </summary>
        public int Breed { get; set; }

        /// <summary>
        /// Stat 59.
        /// </summary>
        public int Sex { get; set; }

        /// <summary>
        /// Stat 89.
        /// </summary>
        public int Race { get; set; }

        /// <summary>
        /// Stat 64. Present on only two of the captured corpses, both humanoid.
        /// Zero means the stat is left out of the message entirely.
        /// </summary>
        public int HeadMesh { get; set; }

        /// <summary>
        /// Stat 34, seconds before the corpse goes. 60 in every capture.
        /// </summary>
        public int DeadTimer { get; set; }

        /// <summary>
        /// A value in the message tail that varies by creature, 502 or 503 in
        /// the captures. Not understood, carried through rather than dropped.
        /// </summary>
        public int Unknown20 { get; set; }

        /// <summary>
        /// As Unknown20. Moves with CatMesh - 297023 against a CatMesh of
        /// 297018 - so it is probably the same model in another form.
        /// </summary>
        public int Unknown23 { get; set; }
    }
}
