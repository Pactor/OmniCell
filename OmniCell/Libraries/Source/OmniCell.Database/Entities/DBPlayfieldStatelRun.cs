namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// One run of an instanced playfield's statels: which client instance ids the statels at a stretch
    /// of the playfield file's own list go by.
    /// </summary>
    /// <remarks>
    /// An instanced playfield names its statels to the client in PlayfieldAnarchyF, as a table of runs
    /// in the file's order - a type, where the run starts, how many, and the instance the first one
    /// gets. The client then calls each statel by that instance: Arete Landing's Surgery Clinic, the
    /// fifteenth Terminal of a run starting at file position 10 from 1477021806, is Terminal:1477021820
    /// (20260909-142713 s3, 20260914-220505 s5). The file itself carries none of these numbers.
    /// </remarks>
    [Tablename("playfieldstatelruns")]
    public class DBPlayfieldStatelRun : IDBEntity
    {
        public int Id { get; set; }

        /// <summary>The playfield template the runs describe, e.g. 6553.</summary>
        public int Playfield { get; set; }

        /// <summary>The order the runs are sent in.</summary>
        public int Ordinal { get; set; }

        /// <summary>The identity type of the statels in the run (Door 51016, Terminal 51005, ...).</summary>
        public int Type { get; set; }

        /// <summary>Position in the playfield file's statel list of the run's first statel.</summary>
        public int StartIndex { get; set; }

        public int Count { get; set; }

        /// <summary>The client instance of the run's first statel; the rest follow on by one.</summary>
        public int FirstInstance { get; set; }
    }
}
