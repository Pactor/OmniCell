CREATE TABLE `playfieldstatelruns` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `Playfield` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL DEFAULT 0,
  `Type` int(32) NOT NULL,
  `StartIndex` int(32) NOT NULL,
  `Count` int(32) NOT NULL,
  `FirstInstance` int(32) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Playfield` (`Playfield`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
