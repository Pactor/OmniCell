CREATE TABLE `questtransitions` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `FromQuest` int(32) NOT NULL,
  `ToQuest` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FromQuest` (`FromQuest`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
