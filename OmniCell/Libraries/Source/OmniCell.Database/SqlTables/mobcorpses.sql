CREATE TABLE  `mobcorpses` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `MobName` varchar(255) NOT NULL,
  `CatMesh` int(32) NOT NULL DEFAULT 0,
  `TimeExist` int(32) NOT NULL DEFAULT 0,
  `Cash` int(32) NOT NULL DEFAULT 0,
  `CanChangeClothes` int(32) NOT NULL DEFAULT 0,
  `MonsterScale` int(32) NOT NULL DEFAULT 100,
  `Breed` int(32) NOT NULL DEFAULT 0,
  `Sex` int(32) NOT NULL DEFAULT 0,
  `Race` int(32) NOT NULL DEFAULT 1,
  `HeadMesh` int(32) NOT NULL DEFAULT 0,
  `DeadTimer` int(32) NOT NULL DEFAULT 60,
  `Unknown20` int(32) NOT NULL DEFAULT 0,
  `Unknown23` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `mob` (`MobName`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
