CREATE TABLE `questwire` (
  `QuestId` int(32) NOT NULL,
  `Source` varchar(32) NOT NULL DEFAULT 'OmniCellDefined',
  `GiverType` int(32) NOT NULL DEFAULT 0,
  `GiverInstance` int(32) NOT NULL DEFAULT 0,
  `QuestCode` int(32) NOT NULL DEFAULT 0,
  `UnknownHash` int(32) NOT NULL DEFAULT 0,
  `Quality` int(32) NOT NULL DEFAULT 0,
  `TimeLimit` int(32) NOT NULL DEFAULT 0,
  `Unknown20` int(32) NOT NULL DEFAULT 6,
  `Unknown21` int(32) NOT NULL DEFAULT 0,
  `Unknown22` int(32) NOT NULL DEFAULT 0,
  `Unknown23Type` int(32) NOT NULL DEFAULT 0,
  `Unknown23Instance` int(32) NOT NULL DEFAULT 0,
  `Unknown25` int(32) NOT NULL DEFAULT 0,
  `Unknown26` int(32) NOT NULL DEFAULT 7,
  PRIMARY KEY (`QuestId`) USING BTREE,
  CONSTRAINT `questwire_quest` FOREIGN KEY (`QuestId`) REFERENCES `quests` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
