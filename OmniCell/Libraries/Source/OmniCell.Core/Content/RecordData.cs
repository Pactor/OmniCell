namespace OmniCell.Core.Content
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// What an item or nano record in the client resource database carries
    /// beyond the fields the game model uses, kept so extraction loses nothing.
    /// </summary>
    /// <remarks>
    /// Filled by the extractor's NewParser, which reads every byte of every
    /// item and nano record (18.8.62_EP1: all 120,842 items and 10,965 nanos).
    /// Nothing in the server reads these yet. They are not MessagePack
    /// serialized: the owning fields are [NonSerialized], so the legacy object
    /// caches still deserialize; the content pack stores them from version 2.
    /// </remarks>
    [Serializable]
    public class RecordData
    {
        /// <summary>
        /// The record's first three int32s. The first is 0xC73D, 0xC74A or
        /// 0xC74E for items and 0xC76B for nanos; the third is always 15.
        /// </summary>
        public int HeaderA;

        public int HeaderB;

        public int HeaderC;

        /// <summary>
        /// The description text, read the same way as the name.
        /// </summary>
        public string Description = string.Empty;

        /// <summary>
        /// Every body block key in record order (2 event, 4 attack/defense,
        /// 6, 14 and 20 animation/sound, 22 actions, 23 shop inventory, 0 padding).
        /// </summary>
        public List<int> BlockOrder = new List<int>();

        /// <summary>
        /// Block 4, every group as stored. Groups 12 and 13 are also what the
        /// model's Attack and Defend dictionaries hold.
        /// </summary>
        public List<AttributeGroup> AttributeGroups = new List<AttributeGroup>();

        /// <summary>
        /// Block 6 (its value is always 0x1B): int32 pairs, flattened key, value.
        /// </summary>
        public List<int> Block6Pairs = new List<int>();

        /// <summary>
        /// Blocks 14 and 20.
        /// </summary>
        public List<AnimSoundSet> AnimSoundSets = new List<AnimSoundSet>();

        /// <summary>
        /// Block 22 as stored: each action's hook and its raw requirement
        /// triples. The model's Actions hold the same requirements cooked.
        /// </summary>
        public List<RequirementSet> ActionRequirements = new List<RequirementSet>();

        /// <summary>
        /// Block 23: each shop inventory event and the raw bytes of its entries.
        /// </summary>
        public List<ShopBlock> ShopBlocks = new List<ShopBlock>();
    }

    [Serializable]
    public class AttributeGroup
    {
        public int Key;

        /// <summary>
        /// int32 pairs, flattened key, value.
        /// </summary>
        public List<int> Pairs = new List<int>();
    }

    [Serializable]
    public class AnimSoundSet
    {
        /// <summary>
        /// 14 or 20.
        /// </summary>
        public int BlockKey;

        public int Value;

        public List<AnimSoundEntry> Entries = new List<AnimSoundEntry>();
    }

    [Serializable]
    public class AnimSoundEntry
    {
        public int Key;

        public List<int> Values = new List<int>();
    }

    [Serializable]
    public class RequirementSet
    {
        public int Hook;

        /// <summary>
        /// int32 triples, flattened stat, value, operator.
        /// </summary>
        public List<int> Triples = new List<int>();
    }

    [Serializable]
    public class ShopBlock
    {
        public int EventType;

        public List<byte[]> Entries = new List<byte[]>();
    }

    /// <summary>
    /// What a function in a record carries beyond the model's Function fields.
    /// </summary>
    [Serializable]
    public class FunctionRecordData
    {
        /// <summary>
        /// Zero int32s read before the function id.
        /// </summary>
        public int LeadingZeroWords;

        /// <summary>
        /// The two int32s after the function id (0 and 4 in every function seen).
        /// </summary>
        public int Header1;

        public int Header2;

        /// <summary>
        /// The int32 after the target, before the arguments.
        /// </summary>
        public int Header3;

        /// <summary>
        /// The raw requirement triples, flattened stat, value, operator.
        /// </summary>
        public List<int> RequirementTriples = new List<int>();

        /// <summary>
        /// Every argument byte exactly as stored, including the bytes
        /// FunctionSets.cfg marks x (not decoded into Arguments).
        /// </summary>
        public byte[] Arguments = new byte[0];
    }
}
