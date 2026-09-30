CREATE TABLE  `npcactivenanos` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `NpcName` varchar(255) NOT NULL,
  `Ordinal` int(32) NOT NULL DEFAULT 0,
  `NanoType` int(32) NOT NULL DEFAULT 53019,
  `NanoId` int(32) NOT NULL DEFAULT 0,
  `Time1` int(32) NOT NULL DEFAULT 0,
  `Time2` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `npcordinal` (`NpcName`, `Ordinal`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
