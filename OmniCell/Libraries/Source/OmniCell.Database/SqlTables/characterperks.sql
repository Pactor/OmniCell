-- The perks a character has trained.
--
-- Retail sends these to the client in FullCharacter.ResearchGoals, one entry
-- per perk, and PerkEntries stays empty - a capture of a Keeper using five
-- perks for 48 minutes had 147 ResearchGoals and no PerkEntries at all.
-- PerkId is the perk's short id: the same number the client sends back, plus
-- ten thousand, when the perk is pressed.
CREATE TABLE `characterperks` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `CharacterId` int(32) NOT NULL,
  `PerkId` int(32) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `CharacterPerk` (`CharacterId`, `PerkId`),
  KEY `CharacterId` (`CharacterId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
