CREATE TABLE `fixturebehaviours` (
  `Template` int(32) NOT NULL,
  `DespawnOnUse` int(1) NOT NULL DEFAULT 0,
  `RespawnSeconds` int(32) NOT NULL DEFAULT 0,
  `Feedback` text NOT NULL,
  PRIMARY KEY (`Template`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
