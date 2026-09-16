namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// How a kind of creature lives, by name: whether it wanders and how often, and whether it starts
    /// fights. Measured from the retail recordings; see SqlPatches/npc-behaviour.sql.
    /// </summary>
    [Tablename("npcbehaviour")]
    public class DBNpcBehaviour : IDBEntity
    {
        public int Id { get; set; }

        public string NpcName { get; set; }

        /// <summary>
        /// Of the creatures of this name seen, the share that walked about at all (0 to 1).
        /// </summary>
        public float WanderShare { get; set; }

        /// <summary>
        /// Seconds from the start of one wander step to the next, low end.
        /// </summary>
        public float WanderPauseMin { get; set; }

        /// <summary>
        /// Seconds from the start of one wander step to the next, high end.
        /// </summary>
        public float WanderPauseMax { get; set; }

        /// <summary>
        /// 1 when it runs its wander steps rather than walking them.
        /// </summary>
        public int WanderRuns { get; set; }

        /// <summary>
        /// 1 when it attacks a player who comes near without being attacked first.
        /// </summary>
        public int Aggressive { get; set; }

        /// <summary>
        /// Metres within which an aggressive creature attacks.
        /// </summary>
        public float AggroRadius { get; set; }
    }

    /// <summary>
    /// A place a creature of this name walked to on the live server, in one playfield.
    /// </summary>
    [Tablename("npcwanderpoints")]
    public class DBNpcWanderPoint : IDBEntity
    {
        public int Id { get; set; }

        public int Playfield { get; set; }

        public string NpcName { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }
    }
}
