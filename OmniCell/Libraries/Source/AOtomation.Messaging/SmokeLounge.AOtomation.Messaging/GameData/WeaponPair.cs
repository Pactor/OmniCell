// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WeaponPair.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the WeaponPair type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One entry of the list SimpleCharFullUpdate carries behind
    /// HasNoWeaponPairs.
    /// </summary>
    /// <remarks>
    /// Four int32s, read by Gamecode.dll 0x10065BCD. The wire order is not the
    /// order the client stores them in - it fills object offsets 0, 4, 0xC and
    /// then 8 - so the third value read lands past the fourth. The names below
    /// follow the wire, which is the only order that matters here.
    ///
    /// The flag it hangs off has been called HasNoWeaponPairs since SmokeLounge
    /// and nothing has confirmed that reading. It is kept because renaming a
    /// flag on no evidence trades one guess for another.
    /// </remarks>
    public class WeaponPair
    {
        #region AoMember Properties

        /// <summary>
        /// The item's low quality template.
        /// </summary>
        /// <remarks>
        /// This and <see cref="ItemHighId"/> are the pair every itemref link
        /// carries, and items.dat settles it rather than the shape suggesting
        /// it: of the eight distinct first entries in the 77 captured lists,
        /// seven are consecutive - 300523 and 300524, 120913 and 120914,
        /// 201062 and 201063 - and looking the first pair up gives quality 1
        /// for 300523 and quality 400 for 300524, which is exactly what a low
        /// and high template are.
        ///
        /// The eighth pair is 43712 and 144745, which are not consecutive.
        /// 43712 is quality 1, so the pair still reads as low and high; the
        /// templates of an item with a range simply need not be adjacent.
        /// </remarks>
        [AoMember(0)]
        public int ItemLowId { get; set; }

        /// <summary>
        /// The item's high quality template. See <see cref="ItemLowId"/>.
        /// </summary>
        [AoMember(1)]
        public int ItemHighId { get; set; }

        /// <summary>
        /// The key this weapon is filed under in the character's weapon
        /// holder. Third on the wire, and stored past the fourth at object
        /// + 0xC.
        /// </summary>
        /// <remarks>
        /// The vector was followed on 2026-09-11. The dispatcher hands it to
        /// the WeaponHolder_t at character + 0x1D4 - 0x1006B43F copies it into
        /// the holder's + 0x2C - and 0x1006B14C then walks it. Per entry it
        /// looks the item up from <see cref="ItemLowId"/>,
        /// <see cref="ItemHighId"/> and the character's level, and then uses
        /// this field twice: 0x1006A166 files the item in a map on the holder
        /// under it as the key, and 0x1006B2A8 compares it with 100 - a match
        /// takes a second path that counts the holder's filled slots and calls
        /// 0x1006A61A, which itself only acts on placements 0x10, 0x30, 0x3D
        /// and 0x3F.
        ///
        /// The map it goes into is the one AttackInfo reads. That message
        /// carries a weapon-instance override, and the combat routine at
        /// 0x1006AE3C branches on it: zero picks the weapon by slot through
        /// 0x100685BB, and anything else goes to 0x10068C19, which looks the
        /// value up in the map at the holder''s + 0xC - the same holder, the
        /// same + 0xC, the map 0x1006A166 inserted into. One packet fills it
        /// and the other reads it, which is what makes this a key rather than
        /// a code that happens to sit here. One of the captured values,
        /// 0x4C455731, appears in an AttackInfo capture in that very field.
        ///
        /// What the values are drawn from is settled too. They are four
        /// printable capitals in seven of the eight captured entries - QKSI,
        /// LEW2, LEW1, DMXF - and none is a string in any client binary
        /// because they live in the resource database: LEW1 is in the record
        /// named Equip_LeetBite1 "LeetBite1 MonsterEquipper", QKSI in
        /// Equip_NPEFireFighterWeapon, DMXF in Equip_PoisonHit. So an NPC''s
        /// weapon is keyed by the equipper record that gave it the weapon.
        ///
        /// The eighth entry is the one carrying 100 rather than a code, and
        /// its item, template 43712, is the only one of the four with
        /// placement (stat 298) of 0 rather than 64.
        /// </remarks>
        [AoMember(2)]
        public int WeaponInstanceKey { get; set; }

        /// <summary>
        /// The four-character code of the record that supplied this weapon.
        /// Fourth on the wire, stored ahead of the third at object + 8.
        /// </summary>
        /// <remarks>
        /// Read as a big-endian int32 every captured value is four printable
        /// capitals, and every one of them names something in the client's own
        /// resource database. Nine are monster equippers: the database holds
        /// the code fifty-odd bytes after the record's name, and the name is
        /// the code spelled out - Equip_LeetBite1 carries LEW1, Equip_LeetBite2
        /// LEW2, Equip_KitevultureClaws2 KIW2, Equip_BureaucratpetRightfist
        /// BUW1, Equip_SingleBreedMonsterWeapon SIW1. The remaining four -
        /// QKSI, DMXF, DBPW, EPAH, AZUS - are arbitrary rather than
        /// abbreviated, and sit beside Equip_NPEFireFighterWeapon,
        /// Equip_PoisonHit, Equip_Bite, Equip_StickToHead and Equip_Arms.
        ///
        /// The templates confirm the pairing rather than the placement
        /// suggesting it. A monster weapon is two database records, X_001 and
        /// X_400, and their ids are the pair this record carries: LEW1 arrives
        /// with 120910 and 120911, which are LeetBite1_001 and LeetBite1_400,
        /// and LEW2 with 120913 and 120914, LeetBite2_001 and LeetBite2_400.
        /// QKSI arrives with 300523 and 300524, NPEFireFighterWeapon_001 and
        /// _400. So the code is the equipper and the templates are the weapon
        /// that equipper grants.
        ///
        /// The last three codes are in no database record, and they are the
        /// ones that settle what this field is for. MAAT arrives with 43712 and
        /// 144745, which the database names "Martial Arts Item"; DIIT with
        /// 42033 and 42032, "Dimach Item b1"; BRAW with 70292 and 70293,
        /// "Brawl Item". Those are the innate attacks every character carries
        /// rather than anything an equipper granted, so no Equip_ record exists
        /// to hold their code and the server spells it out itself - and it
        /// spells it the same way, MAAT for martial arts, DIIT for the dimach
        /// item, BRAW for brawl.
        ///
        /// Those three are also the only entries where
        /// <see cref="WeaponInstanceKey"/> is not this same value: it carries
        /// 100, 144 and 142 there, which are the stat ids of MartialArts,
        /// Dimach and Brawl. So the two fields are not a duplicate that happens
        /// to differ - one is the key the weapon is filed under, which for an
        /// innate attack is its skill, and this one is what supplied it. The
        /// client's `cmp dword ptr [esi + 0x14], 0x64` at 0x1006B2A8 is reading
        /// that: 100 is martial arts, the attack a character falls back to.
        ///
        /// The holder walk at 0x1006B14C never reads this word - the list node
        /// puts the record at + 8, so the walk's [esi+8], [esi+0xc] and
        /// [esi+0x14] are the two templates and the key, and record + 8 is
        /// [esi+0x10], which it does not touch. So the client drops it. What it
        /// means is no longer open.
        /// </remarks>
        [AoMember(3)]
        public int SourceKey { get; set; }

        #endregion
    }
}
