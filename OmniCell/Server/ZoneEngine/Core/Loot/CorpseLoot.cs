namespace ZoneEngine.Core.Loot
{
    using OmniCell.Core.Inventory;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// The server-side inventory behind a corpse shown to the client.
    /// </summary>
    public sealed class CorpseLoot : PooledObject, IItemContainer
    {
        public CorpseLoot(Identity playfield, Identity identity)
            : base(playfield, identity)
        {
            this.BaseInventory = new CorpseInventory(this);
        }

        public IInventoryPages BaseInventory { get; private set; }

        public bool Read()
        {
            return true;
        }

        public bool Write()
        {
            // Corpse contents are deliberately transient.
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.BaseInventory != null)
            {
                this.BaseInventory.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
