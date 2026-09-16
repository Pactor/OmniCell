CREATE TABLE  `npcwanderpoints` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `Playfield` int(32) NOT NULL,
  `NpcName` varchar(255) NOT NULL,
  `X` float NOT NULL,
  `Y` float NOT NULL,
  `Z` float NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `npc` (`Playfield`, `NpcName`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
