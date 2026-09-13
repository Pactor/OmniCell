CREATE TABLE  `mobspawnsmeshs` (
  `Id` int(32) NOT NULL,
  `Playfield` int(32) NOT NULL,
  `Position` int(32) NOT NULL,
  `OverrideTextureId` int(32) NOT NULL,
  `MeshId` int(32) NOT NULL,
  `Layer` int(32) NOT NULL,
  KEY `spawn` (`Playfield`,`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
