CREATE TABLE `knubotopeners` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `Playfield` int(32) NOT NULL,
  `NpcName` varchar(255) NOT NULL,
  `Priority` int(32) NOT NULL DEFAULT 0,
  `RequireActive` varchar(1024) NOT NULL DEFAULT '',
  `RequireDone` varchar(1024) NOT NULL DEFAULT '',
  `ForbidStarted` varchar(1024) NOT NULL DEFAULT '',
  `Node` int(32) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `talker` (`Playfield`,`NpcName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
