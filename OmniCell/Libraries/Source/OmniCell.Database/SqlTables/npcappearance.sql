CREATE TABLE  `npcappearance` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `NpcName` varchar(255) NOT NULL,
  `SecondMonsterScale` int(32) NOT NULL DEFAULT -1,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `npc` (`NpcName`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
