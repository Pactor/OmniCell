CREATE TABLE  `questobjectives` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `QuestId` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL,
  `ObjectiveType` int(32) NOT NULL DEFAULT 0,
  `Target` varchar(255) NOT NULL DEFAULT '',
  `TargetLowId` int(32) NOT NULL DEFAULT 0,
  `TargetHighId` int(32) NOT NULL DEFAULT 0,
  `TargetQuality` int(32) NOT NULL DEFAULT 0,
  `Required` int(32) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `quest_ordinal` (`QuestId`,`Ordinal`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
