CREATE TABLE  `charactersquests` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `CharacterId` int(32) NOT NULL,
  `QuestId` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL,
  `Progress` int(32) NOT NULL DEFAULT 0,
  `State` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `character_quest_ordinal` (`CharacterId`,`QuestId`,`Ordinal`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
