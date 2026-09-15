namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// Where a character's conversation starts, given where the player is in its quests.
    /// </summary>
    /// <remarks>
    /// The quest lists are comma separated stage ids. An opener applies when every RequireActive
    /// stage is in progress, every RequireDone stage is finished and no ForbidStarted stage was ever
    /// begun; of those that apply, the highest Priority wins.
    /// </remarks>
    [Tablename("knubotopeners")]
    public class DBKnuBotOpener : IDBEntity
    {
        public int Id { get; set; }

        public int Playfield { get; set; }

        public string NpcName { get; set; }

        public int Priority { get; set; }

        public string RequireActive { get; set; }

        public string RequireDone { get; set; }

        public string ForbidStarted { get; set; }

        public int Node { get; set; }
    }
}
