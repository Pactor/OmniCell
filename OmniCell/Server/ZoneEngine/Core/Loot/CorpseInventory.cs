namespace ZoneEngine.Core.Loot
{
    using OmniCell.Core.Inventory;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// The captured corpse window has 21 entries numbered from zero.
    /// </summary>
    internal sealed class CorpseInventory : BaseInventoryPages
    {
        public CorpseInventory(IItemContainer owner)
            : base((int)IdentityType.Inventory, owner)
        {
            this.Pages.Add((int)IdentityType.Inventory, new CorpseInventoryPage(owner.Identity));
        }
    }

    internal sealed class CorpseInventoryPage : BaseInventoryPage
    {
        public CorpseInventoryPage(Identity owner)
            : base((int)IdentityType.Inventory, 21, 0, owner)
        {
        }

        public override bool Read()
        {
            return true;
        }

        public override bool Write()
        {
            // Corpse inventory exists only for the lifetime of its body.
            return true;
        }
    }
}
