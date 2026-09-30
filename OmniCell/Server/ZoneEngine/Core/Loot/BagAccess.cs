namespace ZoneEngine.Core.Loot
{
    using System.Collections.Generic;

    using OmniCell.Core.Inventory;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// The open bags of each character, and what is in them.
    /// </summary>
    /// <remarks>
    /// A held bag is a container the client opens like a corpse or a chest, but
    /// its contents live with the character rather than in the world, so they
    /// are kept here rather than in the pool. Each bag has one page, kept by the
    /// bag's Container instance so what you put in stays in across re-opens; a
    /// handle - the per-character number the captures start at 112 and climb -
    /// is what the client addresses an open bag's slots by, Backpack, (handle
    /// &lt;&lt; 16) | slot. See ChestItemFullUpdateMessageHandler.SendForHeldBag,
    /// InventoryUpdateMessageHandler.SendForBag and the container handlers.
    ///
    /// This is in memory for now: a bag's contents last the session but are not
    /// yet written back to the database across logins. That is the next step.
    /// </remarks>
    public static class BagAccess
    {
        // The captures start bag handles at 112, but that is also
        // CorpseLootAccess.CapturedVirtualSlot, and a Backpack move names only
        // the handle - the two would be indistinguishable. The handle value is
        // the server's to choose (the client addresses a bag by whatever it was
        // handed), so bags start clear of the corpse's 112.
        public const int FirstHandle = 200;

        private static readonly object Gate = new object();

        // character -> container instance -> that bag's page.
        private static readonly Dictionary<ulong, Dictionary<int, BackPackInventoryPage>> PagesByCharacter =
            new Dictionary<ulong, Dictionary<int, BackPackInventoryPage>>();

        // character -> open handle -> container instance.
        private static readonly Dictionary<ulong, Dictionary<int, int>> HandleToContainer =
            new Dictionary<ulong, Dictionary<int, int>>();

        private static readonly Dictionary<ulong, int> NextHandle = new Dictionary<ulong, int>();

        /// <summary>
        /// The page of a bag, made the first time the bag is opened and kept
        /// after.
        /// </summary>
        public static BackPackInventoryPage PageFor(Identity character, Identity bag)
        {
            lock (Gate)
            {
                Dictionary<int, BackPackInventoryPage> pages;
                if (!PagesByCharacter.TryGetValue(character.Long(), out pages))
                {
                    pages = new Dictionary<int, BackPackInventoryPage>();
                    PagesByCharacter[character.Long()] = pages;
                }

                BackPackInventoryPage page;
                if (!pages.TryGetValue(bag.Instance, out page))
                {
                    page = new BackPackInventoryPage(bag);
                    pages[bag.Instance] = page;
                }

                return page;
            }
        }

        /// <summary>
        /// Hands out the next open handle for a bag and remembers which bag it
        /// points at, so a move that names the handle finds the right page.
        /// </summary>
        public static int Open(Identity character, Identity bag)
        {
            lock (Gate)
            {
                int handle;
                if (!NextHandle.TryGetValue(character.Long(), out handle) || handle < FirstHandle)
                {
                    handle = FirstHandle;
                }

                NextHandle[character.Long()] = handle + 1;

                Dictionary<int, int> handles;
                if (!HandleToContainer.TryGetValue(character.Long(), out handles))
                {
                    handles = new Dictionary<int, int>();
                    HandleToContainer[character.Long()] = handles;
                }

                handles[handle] = bag.Instance;
                return handle;
            }
        }

        /// <summary>
        /// The page an open handle points at, or null if that handle is not open.
        /// </summary>
        public static BackPackInventoryPage ByHandle(Identity character, int handle)
        {
            lock (Gate)
            {
                Dictionary<int, int> handles;
                Dictionary<int, BackPackInventoryPage> pages;
                int container;
                BackPackInventoryPage page;
                if (HandleToContainer.TryGetValue(character.Long(), out handles)
                    && handles.TryGetValue(handle, out container)
                    && PagesByCharacter.TryGetValue(character.Long(), out pages)
                    && pages.TryGetValue(container, out page))
                {
                    return page;
                }

                return null;
            }
        }

        /// <summary>
        /// The page of a bag by its Container instance, or null if it was never
        /// opened this session.
        /// </summary>
        public static BackPackInventoryPage ByContainer(Identity character, int containerInstance)
        {
            lock (Gate)
            {
                Dictionary<int, BackPackInventoryPage> pages;
                BackPackInventoryPage page;
                if (PagesByCharacter.TryGetValue(character.Long(), out pages)
                    && pages.TryGetValue(containerInstance, out page))
                {
                    return page;
                }

                return null;
            }
        }
    }
}
