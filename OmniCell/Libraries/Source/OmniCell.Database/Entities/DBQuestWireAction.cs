namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// One normalized QuestActionList entry.
    /// </summary>
    [Tablename("questwireactions")]
    public class DBQuestWireAction : IDBEntity
    {
        public int Id { get; set; }
        public int QuestId { get; set; }
        public int Ordinal { get; set; }
        public int Version { get; set; }
        public int ActionType { get; set; }
        public int ActionInstance { get; set; }
        public int Unknown1Type { get; set; }
        public int Unknown1Instance { get; set; }
        public int Unknown2Type { get; set; }
        public int Unknown2Instance { get; set; }
        public int Unknown3Type { get; set; }
        public int Unknown3Instance { get; set; }
        public int Unknown4Type { get; set; }
        public int Unknown4Instance { get; set; }
        public float Unknown5 { get; set; }
        public float Unknown6 { get; set; }
        public float Unknown7 { get; set; }
        public float Unknown8 { get; set; }
        public int Unknown9Type { get; set; }
        public int Unknown9Instance { get; set; }
        public float Unknown10 { get; set; }
        public float Unknown11 { get; set; }
        public float Unknown12 { get; set; }
        public float Unknown13 { get; set; }
        public int Unknown14Type { get; set; }
        public int Unknown14Instance { get; set; }
        public int Deadline { get; set; }
        public int Unknown16 { get; set; }
        public int TrackingType { get; set; }
        public int PlayfieldType { get; set; }
        public int PlayfieldInstance { get; set; }
        public int Unknown18 { get; set; }
        public int Unknown19 { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
