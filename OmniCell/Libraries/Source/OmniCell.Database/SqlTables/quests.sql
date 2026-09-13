CREATE TABLE  `quests` (
  `Id` int(32) NOT NULL,
  `Name` varchar(255) NOT NULL DEFAULT '',
  `Description` text,
  `GiverId` int(32) NOT NULL DEFAULT 0,
  `IconId` int(32) NOT NULL DEFAULT 0,
  `CashReward` int(32) NOT NULL DEFAULT 0,
  `ExperienceReward` int(32) NOT NULL DEFAULT 0,
  `Playfield` int(32) NOT NULL DEFAULT 0,
  `Requires` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
