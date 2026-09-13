namespace SmokeLounge.AOtomation.Messaging.GameData
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// </summary>
    public class FollowTargetInfo : FollowInfo
    {
        private byte followInfoType = 2;

        /// <summary>
        /// </summary>
        [AoMember(0)]
        public byte FollowInfoType
        {
            get
            {
                return this.followInfoType;
            }
            set
            {
                this.followInfoType = value;
            }
        }

        public FollowTargetInfo()
        {
            this.MoveType = 0;
        }

        /// <summary>
        /// DataLength is 0 for FollowTargetInfo
        /// </summary>
        [AoMember(1)]
        public byte MoveType { get; set; }

        /// <summary>
        /// </summary>
        [AoMember(2)]
        public Identity Target { get; set; }

        /// <summary>
        /// A float between the target and the position. 2.0 in almost every
        /// captured copy.
        /// </summary>
        /// <remarks>
        /// This was a byte called Dummy followed by three bytes the reader threw
        /// away as padding. It is one float: the client reads it at Gamecode
        /// 0x10073740 into the follow object's + 0x20 and writes it back from
        /// there at 0x10073623, and neither side has padding in the record.
        ///
        /// The mistake survived 27,466 captured copies because the value is
        /// exactly 2.0 in all of them, and 2.0 is 0x40000000 - a first byte of
        /// 0x40 and three zeros, which is exactly what writing the byte and three
        /// zeros produces. The two copies that broke it are 2.5, and they came
        /// back 2.0.
        ///
        /// What it means is not settled. The dispatcher at 0x1007382C reads the
        /// move type, the position and the coordinate list and never touches
        /// this, and the coordinate-follow branch of the reader zeroes it rather
        /// than reading one. A float between a follow target and a position, at
        /// 2.0 and occasionally 2.5, reads like a distance to keep - but nothing
        /// in the client says so, so it is not claimed.
        /// </remarks>
        [AoMember(3)]
        public float Unknown1 { get; set; }
        [AoMember(4)]
        public float X { get; set; }

        /// <summary>
        /// </summary>
        [AoMember(5)]
        public float Y { get; set; }

        /// <summary>
        /// </summary>
        [AoMember(6)]
        public float Z { get; set; }

        [AoMember(7)]
        public byte CoordinateCount { get; set; }

        [AoMember(8)]
        public Vector3[] Coordinates { get; set; }
    }
}
