#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// Items granted by a SpawnItem key, and granting them.
    /// </summary>
    /// <remarks>
    /// The keys come from XML Data/ItemSpawns.xml, where every row names the
    /// capture it was read from. A key not in that file is not an item key
    /// this server knows, and SpawnItem treats it as a pet, as before.
    ///
    /// What the client is told, per item, is the live server's order when a
    /// package is opened: TemplateAction with Unknown2 87 at OverflowWindow:0,
    /// then ContainerAddItem from OverflowWindow:0 to the player's overflow
    /// window at placement 0x6F. That pair repeats once per item in
    /// 20260911-171203_s12 (seq 1590-1604, 1617-1632) and 20260909-141545 (seq
    /// 729-730, 2848-2855). The item itself goes to the first free inventory
    /// slot: after 171203 seq 1634 the client equipped the eight items from
    /// Inventory 66, 78, 79, 80, 81, 82, 83 and 84, in grant order, 66 being the
    /// slot freed by the package opened just before.
    /// </remarks>
    public static class ItemSpawns
    {
        #region Static Fields

        private static readonly Dictionary<string, List<int>> Items = new Dictionary<string, List<int>>();

        #endregion

        #region Public Methods and Operators

        public static int Load()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "XML Data", "ItemSpawns.xml");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Missing SpawnItem key data.", path);
            }

            XElement root = XDocument.Load(path).Root;
            if (root == null)
            {
                throw new InvalidDataException("ItemSpawns.xml has no root element.");
            }

            var loaded = new Dictionary<string, List<int>>();
            foreach (XElement spawn in root.Elements("Spawn"))
            {
                string key = (string)spawn.Attribute("Key");
                int item;
                if (string.IsNullOrEmpty(key) || key.Length != 4)
                {
                    throw new InvalidDataException("ItemSpawns.xml has a Spawn without a four character Key.");
                }

                if (!int.TryParse((string)spawn.Attribute("Item"), out item) || !ItemLoader.ItemList.ContainsKey(item))
                {
                    throw new InvalidDataException("ItemSpawns.xml key " + key + " names an item that is not in items.ocp.");
                }

                List<int> items;
                if (!loaded.TryGetValue(key, out items))
                {
                    loaded[key] = items = new List<int>();
                }

                items.Add(item);
            }

            lock (Items)
            {
                Items.Clear();
                foreach (KeyValuePair<string, List<int>> pair in loaded)
                {
                    Items[pair.Key] = pair.Value;
                }
            }

            return loaded.Count;
        }

        public static bool Knows(string key)
        {
            lock (Items)
            {
                return key != null && Items.ContainsKey(key);
            }
        }

        /// <summary>
        /// Puts the items a key grants into a character's inventory and tells its
        /// client.
        /// </summary>
        /// <param name="quality">
        /// SpawnItem's second argument. Every packaged key in the captures
        /// carries 1 and granted a quality 1 item; the item still takes whatever
        /// quality its template allows.
        /// </param>
        public static bool Grant(ICharacter character, string key, int quality)
        {
            List<int> items;
            lock (Items)
            {
                if (character == null || key == null || !Items.TryGetValue(key, out items))
                {
                    return false;
                }

                items = new List<int>(items);
            }

            IInventoryPage page = character.BaseInventory.Pages[character.BaseInventory.StandardPage];
            foreach (int id in items)
            {
                int slot = page.FindFreeSlot();
                if (slot < 0)
                {
                    // What live does with a full inventory here is not in any
                    // capture, so nothing is invented for it.
                    Console.WriteLine("SpawnItem " + key + ": no free inventory slot for item " + id + ".");
                    return false;
                }

                ItemTemplate template = ItemLoader.ItemList[id];
                var item = new Item(quality, template.GetLowId(quality), template.GetHighId(quality));
                page.Add(slot, item);

                TemplateActionMessageHandler.Default.SendToOverflow(character, item);
                ContainerAddItemMessageHandler.Default.SendFromOverflow(character);
            }

            return true;
        }

        #endregion
    }
}
