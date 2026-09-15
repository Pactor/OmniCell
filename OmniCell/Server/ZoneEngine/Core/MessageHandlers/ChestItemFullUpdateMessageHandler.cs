#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Playfields;

    #endregion

    /// <summary>
    /// A container the client can open - a chest, or the loot on a corpse.
    /// </summary>
    /// <remarks>
    /// 149 in the captures. Shaped like the vending machine update: an owner, a
    /// position, and a stat list saying what the thing is.
    ///
    /// Sent for a corpse straight after the corpse itself, which is what makes
    /// the body clickable. A corpse with nothing on it is still worth sending -
    /// the client opens an empty window rather than doing nothing, which is what
    /// the live game does too.
    ///
    /// MsgVersion 11 and Identitytype 50000 are the same in every capture. The
    /// inventory id is 3 in the captured chests and the body location 19, and
    /// both are sent as that.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class ChestItemFullUpdateMessageHandler :
        BaseMessageHandler<ChestItemFullUpdateMessage, ChestItemFullUpdateMessageHandler>
    {
        #region Constants

        private const int Version = 11;

        /// <summary>
        /// 50000 in every capture, the same value corpsetype carries.
        /// </summary>
        private const int ContainerType = 50000;

        /// <summary>
        /// Stat 55, inventoryid. 3 in every captured chest.
        /// </summary>
        private const byte InventoryIdConstant = 3;

        /// <summary>
        /// Stat 220, currbodylocation. 19 in every captured chest.
        /// </summary>
        private const byte BodyLocationConstant = 19;

        /// <summary>
        /// Flags of a captured chest.
        /// </summary>
        private const uint FlagsConstant = 67108865;

        #endregion

        #region Outbound

        /// <summary>
        /// Announces the loot container of a corpse.
        /// </summary>
        public void SendForCorpse(ICharacter victim, Identity corpse, int cash)
        {
            this.Send(victim, Filler(victim, corpse, cash), true);
        }

        private static MessageDataFiller Filler(ICharacter victim, Identity corpse, int cash)
        {
            return message =>
            {
                message.Identity = corpse;
                message.Unknown = 0;

                message.Owner = victim.Identity;
                message.MsgVersion = Version;
                message.Identitytype = ContainerType;
                message.Instance = corpse.Instance;

                OmniCell.Core.Vector.Coordinate where = victim.Coordinates();
                message.Coordinates = new Vector3 { X = where.x, Y = where.y, Z = where.z };

                OmniCell.Core.Vector.Quaternion heading = victim.Heading;
                message.Heading = new Quaternion
                                  {
                                      X = heading.xf,
                                      Y = heading.yf,
                                      Z = heading.zf,
                                      W = heading.wf
                                  };

                // The instance, like everything else the client is told about
                // this playfield. See PlayfieldAnarchyFMessageHandler.
                message.PlayfieldId = Playfields.GetClientInstance(victim.Playfield.Identity.Instance);

                // Identity.None went out here for years. Every one of the 167
                // captured chests carries the shared item-message constant
                // instead, and so do all 747 SimpleItemFullUpdates and all 690
                // VendingMachineFullUpdates - which are the same wire format as
                // this one.
                message.Marker = new Identity
                                 {
                                     Type = (IdentityType)ItemMessageConstants.ItemMessageMarker,
                                     Instance = 0
                                 };

                message.InventoryId = InventoryIdConstant;
                message.BodyLocation = BodyLocationConstant;

                message.Stats = new[]
                                {
                                    Stat(StatIds.flags, FlagsConstant),
                                    Stat(StatIds.cash, (uint)(cash < 0 ? 0 : cash))
                                };

                message.Name = "Remains of " + victim.Name;

                // The lockable tail: 2, lockdifficulty 50, no keyholders, 3 - the same in every captured
                // chest (see ChestItemFullUpdateMessage). Left unset the serializer failed on the null
                // keyholder list and no corpse container ever reached the client.
                message.TailVersion = 2;
                message.LockDifficulty = 50;
                message.Keyholders = new Identity[0];
                message.TailEndVersion = 3;
            };
        }

        private static GameTuple<CharacterStat, uint> Stat(StatIds stat, uint value)
        {
            return new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)stat, Value2 = value };
        }

        #endregion
    }
}
