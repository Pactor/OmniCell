CREATE TABLE IF NOT EXISTS `moblootprofiles` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `Playfield` int(32) NOT NULL DEFAULT 0,
  `MobName` varchar(255) NOT NULL,
  `DropHash` varchar(32) NOT NULL,
  `Rolls` int(10) unsigned NOT NULL,
  `Chance` int(10) unsigned NOT NULL COMMENT 'captured basis points per roll; do not estimate',
  PRIMARY KEY (`Id`),
  KEY `mob_identity` (`Playfield`,`MobName`),
  KEY `drop_pool` (`DropHash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
