CREATE TABLE `questwirerewards` (
  `QuestId` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL DEFAULT 0,
  `LowId` int(32) NOT NULL,
  `HighId` int(32) NOT NULL,
  `Quality` int(32) NOT NULL,
  `Unknown1` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`QuestId`,`Ordinal`) USING BTREE,
  CONSTRAINT `questwirerewards_quest` FOREIGN KEY (`QuestId`) REFERENCES `quests` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
