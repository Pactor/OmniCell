namespace ZoneEngine.Core.Loot
{
    using System.Collections.Generic;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Maps the captured client-side corpse bag slot to the transient inventory
    /// currently opened by a character.
    /// </summary>
    public static class CorpseLootAccess
    {
        // Captured InventoryUpdate uses 112 and captured MoveItem encodes the
        // first entry as Backpack 0x00700000: (112 << 16) | item slot 0.
        public const int CapturedVirtualSlot = 112;

        private static readonly object Sync = new object();

        private static readonly Dictionary<ulong, OpenCorpse> OpenByCharacter =
            new Dictionary<ulong, OpenCorpse>();

        public static int Open(ICharacter character, CorpseLoot corpse)
        {
            if (character == null || corpse == null)
            {
                return -1;
            }

            lock (Sync)
            {
                OpenByCharacter[character.Identity.Long()] =
                    new OpenCorpse { Playfield = corpse.Parent, Corpse = corpse.Identity };
            }

            return CapturedVirtualSlot;
        }

        /// <summary>
        /// Whether the character has this corpse open. The client uses a corpse once to open it and
        /// again to close it (retail: two uses per looted corpse in 222 of 264 cases).
        /// </summary>
        public static bool IsOpen(ICharacter character, Identity corpse)
        {
            if (character == null)
            {
                return false;
            }

            lock (Sync)
            {
                OpenCorpse open;
                return OpenByCharacter.TryGetValue(character.Identity.Long(), out open) && open.Corpse == corpse;
            }
        }

        /// <summary>
        /// Whether a corpse has nothing left in it.
        /// </summary>
        public static bool IsEmpty(CorpseLoot corpse)
        {
            return corpse == null || corpse.BaseInventory[corpse.BaseInventory.StandardPage].List().Count == 0;
        }

        public static bool TryTake(
            ICharacter character,
            Identity encodedSource,
            out int destinationSlot,
            out IItem taken)
        {
            destinationSlot = -1;
            taken = null;
            if (character == null
                || encodedSource.Type != IdentityType.Backpack
                || (encodedSource.Instance >> 16) != CapturedVirtualSlot)
            {
                return false;
            }

            OpenCorpse open;
            lock (Sync)
            {
                if (!OpenByCharacter.TryGetValue(character.Identity.Long(), out open))
                {
                    return false;
                }
            }

            CorpseLoot corpse = Pool.Instance.GetObject<CorpseLoot>(open.Playfield, open.Corpse);
            if (corpse == null)
            {
                ForgetCharacter(character.Identity);
                return false;
            }

            IInventoryPage source = corpse.BaseInventory[corpse.BaseInventory.StandardPage];
            IInventoryPage destination = character.BaseInventory[character.BaseInventory.StandardPage];
            int sourceSlot = encodedSource.Instance & 0xFFFF;

            lock (corpse)
            {
                IItem item = source[sourceSlot];
                int free = destination.FindFreeSlot();
                if (item == null || free < 0)
                {
                    return false;
                }

                source.Remove(sourceSlot);
                if (destination.Add(free, item) != InventoryError.OK)
                {
                    source.Add(sourceSlot, item);
                    return false;
                }

                destination.Write();
                destinationSlot = free;
                taken = item;
                return true;
            }
        }

        public static void ForgetCharacter(Identity character)
        {
            lock (Sync)
            {
                OpenByCharacter.Remove(character.Long());
            }
        }

        public static void ForgetCorpse(Identity playfield, Identity corpse)
        {
            lock (Sync)
            {
                var remove = new List<ulong>();
                foreach (KeyValuePair<ulong, OpenCorpse> entry in OpenByCharacter)
                {
                    if (entry.Value.Playfield == playfield && entry.Value.Corpse == corpse)
                    {
                        remove.Add(entry.Key);
                    }
                }

                foreach (ulong character in remove)
                {
                    OpenByCharacter.Remove(character);
                }
            }
        }

        private sealed class OpenCorpse
        {
            public Identity Playfield { get; set; }

            public Identity Corpse { get; set; }
        }
    }
}
