CREATE TABLE `questitemrewards` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `QuestId` int(32) NOT NULL,
  `ItemId` int(32) NOT NULL,
  `Quantity` int(32) NOT NULL DEFAULT 1,
  `GrantOnAccept` int(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `QuestId` (`QuestId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
