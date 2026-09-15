CREATE TABLE `knubotdialogue` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `Playfield` int(32) NOT NULL,
  `NpcName` varchar(255) NOT NULL,
  `Node` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL,
  `Kind` int(32) NOT NULL DEFAULT 0,
  `Text` text NOT NULL,
  `Flag` int(32) NOT NULL DEFAULT 0,
  `Next` int(32) NOT NULL DEFAULT 0,
  `AnswerKind` int(32) NOT NULL DEFAULT 0,
  `ActionValue` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `talker` (`Playfield`,`NpcName`,`Node`,`Ordinal`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
