CREATE TABLE  `npcbehaviour` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `NpcName` varchar(255) NOT NULL,
  `WanderShare` float NOT NULL DEFAULT 0,
  `WanderPauseMin` float NOT NULL DEFAULT 3,
  `WanderPauseMax` float NOT NULL DEFAULT 6,
  `WanderRuns` int(32) NOT NULL DEFAULT 0,
  `Aggressive` int(32) NOT NULL DEFAULT 0,
  `AggroRadius` float NOT NULL DEFAULT 12,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `npc` (`NpcName`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
