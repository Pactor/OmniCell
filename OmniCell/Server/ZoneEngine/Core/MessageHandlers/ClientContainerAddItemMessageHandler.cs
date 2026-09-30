namespace ZoneEngine.Core.MessageHandlers
{
    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Network;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Loot;

    /// <summary>
    /// Putting an item from the main inventory into a held bag.
    /// </summary>
    /// <remarks>
    /// The client sends this with Container set to the bag (its Container
    /// identity) and Item set to the main-inventory slot the thing is in. The
    /// move is confirmed with a ContainerAddItem from that slot into the bag -
    /// the same message AOSharp reads back as OnContainerAddItem(source, target,
    /// slot). Taking an item back out is a MoveItem, handled in
    /// MoveItemMessageHandler. Contents are held in BagAccess for the session.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class ClientContainerAddItemMessageHandler :
        BaseMessageHandler<ClientContainerAddItemMessage, ClientContainerAddItemMessageHandler>
    {
        protected override void Read(ClientContainerAddItemMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            ICharacter character = client.Controller.Character;

            // Only a held bag is answered here; any other container keeps its
            // own path.
            if (message.Container.Type != IdentityType.Container)
            {
                return;
            }

            BackPackInventoryPage bag = BagAccess.ByContainer(character.Identity, message.Container.Instance);
            if (bag == null)
            {
                // The bag was never opened this session, so there is nowhere to
                // put it yet. The open reads the contents; without it a put has
                // no page.
                return;
            }

            IInventoryPage source;
            if (!character.BaseInventory.Pages.TryGetValue((int)message.Item.Type, out source))
            {
                return;
            }

            IItem item = source[message.Item.Instance];
            if (item == null)
            {
                return;
            }

            int slot = bag.FindFreeSlot();
            if (slot < 0)
            {
                return;
            }

            source.Remove(message.Item.Instance);
            bag.Add(slot, item);

            character.Send(
                new ContainerAddItemMessage
                {
                    Identity = character.Identity,
                    Unknown = 0,
                    SourceContainer = message.Item,
                    Target = message.Container,
                    TargetPlacement = slot
                });
        }
    }
}
