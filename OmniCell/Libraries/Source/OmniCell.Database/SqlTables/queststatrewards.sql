CREATE TABLE `queststatrewards` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `QuestId` int(32) NOT NULL,
  `Stat` int(32) NOT NULL,
  `Value` int(32) NOT NULL DEFAULT 0,
  `SetBits` int(1) NOT NULL DEFAULT 1,
  `GrantOnAccept` int(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `QuestId` (`QuestId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
