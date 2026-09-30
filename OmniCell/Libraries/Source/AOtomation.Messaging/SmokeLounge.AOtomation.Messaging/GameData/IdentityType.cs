// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IdentityType.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the IdentityType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    public enum IdentityType
    {
        None = 0, 

        WeaponPage = 0x00000065, 

        ArmorPage = 0x00000066, 

        ImplantPage = 0x00000067, 

        Inventory = 0x00000068, 

        Bank = 0x00000069, 

        Backpack = 0x0000006B, 

        // On Trade this will be the bag for the Character's items to sell
        KnuBotTradeWindow = 0x0000006C, 

        OverflowWindow = 0x0000006E, 

        // On Trade this will be the bag for the Items to buy
        TradeWindow = 0x0000006F, 

        SocialPage = 0x00000073, 

        ShopInventory = 0x00000767, 

        PlayerShopInventory = 0x00000790, 

        Playfield2 = 0x00009C50, 

        CanbeAffected = 0x0000C350,

        CityController = 0x0000C418,

        Terminal = 0x0000C73D,

        Door = 0x0000C748,

        // A player-held bag's own identity. Its contents are addressed as
        // Backpack, (handle << 16) | slot. Chests and lootable corpses carry
        // this type too.
        Container = 0x0000C749,

        WeaponInstance = 0x0000C74A,

        VendingMachine = 0x0000C75B, 

        TempBag = 0x0000C767, 

        Corpse = 0x0000C76A,

        MailTerminal = 0x0000C773,
        
        Playfield1 = 0x0000C79C, 

        Playfield = 0x0000C79D, 

        NanoProgram = 0x0000CF1B, 

        GfxEffect = 0x0000CF26,

        Quest = 0x0000DAC3,

        MissionEntrance = 0x0000DAC6,

        /// <summary>
        /// A mission terminal.
        /// </summary>
        /// <remarks>
        /// 56001. This was 0x0000DCA1 until 2026-09-26, which is the same four
        /// digits with two of them swapped and appears in no capture. Every
        /// captured roll names its terminal 56001 - QuestAlternative's
        /// MissionTerminalIdentity and the offers' QuestGiver both - which is
        /// 0xDAC1, two below Quest at 0xDAC3 and five below MissionEntrance at
        /// 0xDAC6. The mission family is one run of values and this is the
        /// front of it.
        /// </remarks>
        MissionTerminal = 0x0000DAC1,

        TeamWindow = 0x0000DEA9, 

        Organization = 0x0000DEAA, 

        IncomingTradeWindow = 0x0000DEAD, 

        Playfield3 = 0x000186A1,
    }
}