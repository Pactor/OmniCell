namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// What a kind of creature looks like beyond its MonsterData and scale, by name.
    /// </summary>
    /// <remarks>
    /// Read out of every NPC SimpleCharFullUpdate in the retail recordings, the most common form for
    /// each name. A Garbage Flea is MonsterData 17657 with the texture "Material #9" 95883 laid over
    /// it; without the texture the client draws nothing at all. The dynamic ("mini boss") creatures
    /// carry a second monster scale (40 on every one seen) and their glow as active nanos.
    /// </remarks>
    [Tablename("npcappearance")]
    public class DBNpcAppearance : IDBEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The creature's name, as on its spawns and corpses.
        /// </summary>
        public string NpcName { get; set; }

        /// <summary>
        /// The byte SimpleCharFullUpdate carries behind its second monster scale flag, or -1 when the
        /// message leaves it out. The client stores it as monster scale (stat 360) in its other stat
        /// map; 40 on the Cleanmeister Intelligence Robot, the Supreme Collector of Waste, the Mutated
        /// Garbage Flea and the IIV-X Advanced Docker.
        /// </summary>
        public int SecondMonsterScale { get; set; }
    }

    /// <summary>
    /// One texture a kind of creature wears over its model: SimpleCharFullUpdate's extended textures,
    /// and the meshes of the corpse it leaves.
    /// </summary>
    [Tablename("npctextures")]
    public class DBNpcTexture : IDBEntity
    {
        public int Id { get; set; }

        public string NpcName { get; set; }

        /// <summary>
        /// Position in the list, from 0.
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// The material it replaces on the model - "Material #9", "mdrone1" - up to 32 bytes.
        /// </summary>
        public string MaterialName { get; set; }

        public int TextureId { get; set; }

        public int OverlayId { get; set; }

        public int AlphaMode { get; set; }
    }

    /// <summary>
    /// One nano effect a kind of creature shows from the moment it is seen: the glow of a dynamic
    /// creature.
    /// </summary>
    [Tablename("npcactivenanos")]
    public class DBNpcActiveNano : IDBEntity
    {
        public int Id { get; set; }

        public string NpcName { get; set; }

        public int Ordinal { get; set; }

        /// <summary>
        /// The nano's identity type: 53019, a nano program, on every one seen.
        /// </summary>
        public int NanoType { get; set; }

        public int NanoId { get; set; }

        public int Time1 { get; set; }

        public int Time2 { get; set; }
    }
}
