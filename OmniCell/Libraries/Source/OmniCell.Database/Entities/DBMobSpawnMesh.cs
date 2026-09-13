#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Database.Dao
{
    #region Usings

    using OmniCell.Database.Entities;

    #endregion

    /// <summary>
    /// One entry of a spawned character's mesh list.
    /// </summary>
    /// <remarks>
    /// What the client draws a monster or an NPC as. A player's meshes are built
    /// from what it is wearing; a spawned character has no inventory, so the
    /// live server sends a list that belongs to the spawn itself, and this is
    /// where that list is kept.
    ///
    /// The table existed and was empty, with three unnamed value columns and a
    /// primary key on Id, which cannot be right for a spawn that has several
    /// meshes. Nothing had ever written to it or read from it, so it is redefined
    /// here to hold what the captures actually carry: the four fields of a mesh
    /// entry, several rows per spawn.
    /// </remarks>
    [Tablename("mobspawnsmeshs")]
    public class DBMobSpawnMesh : IDBEntity
    {
        #region Public Properties

        /// <summary>
        /// The spawn this mesh belongs to.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The playfield the spawn is in.
        /// </summary>
        public int Playfield { get; set; }

        /// <summary>
        /// Where on the character the mesh goes.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// A texture to draw it with instead of its own, or zero.
        /// </summary>
        public int OverrideTextureId { get; set; }

        /// <summary>
        /// The mesh itself.
        /// </summary>
        public int MeshId { get; set; }

        /// <summary>
        /// Which layer it is drawn on.
        /// </summary>
        public int Layer { get; set; }

        #endregion
    }
}
