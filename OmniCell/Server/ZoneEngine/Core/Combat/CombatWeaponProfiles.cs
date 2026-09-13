#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Combat
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Loads the normalized weapon-holder descriptors used by combat packets.
    /// </summary>
    public static class CombatWeaponProfiles
    {
        private static SpecialAttack[] playerInnate = new SpecialAttack[0];

        private static int[] playerWeaponSlots = new int[0];

        public static int DefaultAttackSlot { get; private set; }

        public static int DefaultInventoryId { get; private set; }

        public static IEnumerable<SpecialAttack> PlayerInnate
        {
            get { return playerInnate.Select(Clone); }
        }

        public static IEnumerable<int> PlayerWeaponSlots
        {
            get { return playerWeaponSlots.ToArray(); }
        }

        public static bool IsPlayerWeaponSlot(int placement)
        {
            return playerWeaponSlots.Contains(placement);
        }

        public static int Load()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "XML Data", "CombatWeapons.xml");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Missing normalized combat weapon data.", path);
            }

            var loaded = new List<SpecialAttack>();
            XElement root = XDocument.Load(path).Root;
            if (root == null)
            {
                throw new InvalidDataException("CombatWeapons.xml has no root element.");
            }

            DefaultAttackSlot = Integer(root, "DefaultAttackSlot");
            DefaultInventoryId = Integer(root, "DefaultInventoryId");

            playerWeaponSlots = root.Elements("PlayerWeaponSlot")
                                    .Select(row => Integer(row, "Placement"))
                                    .Distinct()
                                    .ToArray();
            if (playerWeaponSlots.Length == 0 || !playerWeaponSlots.Contains(DefaultAttackSlot))
            {
                throw new InvalidDataException(
                    "CombatWeapons.xml must define its DefaultAttackSlot as a PlayerWeaponSlot.");
            }

            foreach (XElement row in root.Elements("PlayerInnate"))
            {
                string code = Required(row, "AttackCode");
                if (code.Length != 4)
                {
                    throw new InvalidDataException("Combat weapon AttackCode must contain exactly four characters.");
                }

                loaded.Add(
                    new SpecialAttack
                    {
                        LowTemplateId = Integer(row, "LowTemplateId"),
                        HighTemplateId = Integer(row, "HighTemplateId"),
                        AttackSelector = Integer(row, "AttackSelector"),
                        AttackCode = code
                    });
            }

            if (loaded.Count == 0)
            {
                throw new InvalidDataException("CombatWeapons.xml contains no PlayerInnate descriptors.");
            }

            playerInnate = loaded.ToArray();
            return playerInnate.Length;
        }

        private static int Integer(XElement row, string name)
        {
            int value;
            if (!int.TryParse(Required(row, name), out value))
            {
                throw new InvalidDataException("Combat weapon " + name + " is not an integer.");
            }

            return value;
        }

        private static string Required(XElement row, string name)
        {
            XAttribute attribute = row.Attribute(name);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
            {
                throw new InvalidDataException("Combat weapon row is missing " + name + ".");
            }

            return attribute.Value;
        }

        private static SpecialAttack Clone(SpecialAttack source)
        {
            return new SpecialAttack
                   {
                       LowTemplateId = source.LowTemplateId,
                       HighTemplateId = source.HighTemplateId,
                       AttackSelector = source.AttackSelector,
                       AttackCode = source.AttackCode
                   };
        }
    }
}
