CREATE TABLE  `npctextures` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `NpcName` varchar(255) NOT NULL,
  `Ordinal` int(32) NOT NULL DEFAULT 0,
  `MaterialName` varchar(32) NOT NULL DEFAULT '',
  `TextureId` int(32) NOT NULL DEFAULT 0,
  `OverlayId` int(32) NOT NULL DEFAULT 0,
  `AlphaMode` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `npcordinal` (`NpcName`, `Ordinal`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
