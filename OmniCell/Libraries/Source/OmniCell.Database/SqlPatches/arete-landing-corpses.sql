-- What each kind of creature leaves behind, read out of the corpses the
-- live server sent. Everything that differs between one creature and
-- another; the values that are the same for all of them are constants in
-- CorpseFullUpdateMessageHandler.
--
-- CatMesh is the corpse model, DeadTimer how long it lies there, Cash
-- what is on it. DeathAnimation and MonsterData are the two creature values
-- in the corpse's effect (they were Unknown20 and Unknown23, stored
-- byte-swapped).

-- The columns under their names, renamed once. SqlTables/mobcorpses.sql has them for a fresh database.
SET @oldNames := (
  SELECT COUNT(*) FROM information_schema.columns
  WHERE table_schema = DATABASE() AND table_name = 'mobcorpses' AND column_name = 'Unknown20');
SET @rename := IF(
  @oldNames = 0,
  'DO 0',
  'ALTER TABLE mobcorpses CHANGE COLUMN `Unknown20` `DeathAnimation` int(32) NOT NULL DEFAULT 0, CHANGE COLUMN `Unknown23` `MonsterData` int(32) NOT NULL DEFAULT 0');
PREPARE renameColumns FROM @rename;
EXECUTE renameColumns;
DEALLOCATE PREPARE renameColumns;

REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Ace Adventurer', 5914, 18000, 6145, 1, 129, 2, 2, 1, 20069, 60, 500, 26139);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Ace Assassin', 5927, 18000, 8979, 1, 128, 1, 3, 1, 20082, 60, 500, 26090);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Ace Clan Bureaucrat', 5941, 18000, 8900, 1, 128, 3, 3, 1, 20014, 60, 500, 26149);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Ace Disavowed Medic', 17909, 18000, 5564, 1, 127, 3, 2, 1, 20030, 60, 500, 26159);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Ace Marksman', 23368, 18000, 16373, 1, 126, 3, 3, 1, 20016, 60, 500, 26143);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Ace Rogue', 17528, 18000, 16532, 1, 127, 1, 3, 1, 40634, 60, 500, 26082);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Alarm Sentry', 210200, 18000, 12004, 0, 100, 6, 0, 1, 0, 60, 500, 210238);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Architect Striker', 17870, 18000, 85, 1, 97, 1, 2, 1, 40698, 60, 501, 203743);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Bureaucrat Worker', 96024, 18000, 0, 0, 92, 7, 1, 1, 0, 60, 503, 96056);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Cleaning Robot', 297018, 18000, 5, 0, 200, 6, 1, 1, 0, 60, 503, 297023);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Cleanmeister Intelligence Robot', 297018, 157200, 0, 0, 640, 6, 1, 1, 0, 60, 500, 297023);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Daft Clan Diversionist', 23378, 18000, 17, 1, 91, 2, 2, 1, 20074, 60, 501, 26135);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Discarded Pet', 15929, 18000, 18, 0, 93, 6, 0, 1, 0, 60, 503, 17720);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Disobedient Bot', 15215, 18000, 12, 0, 95, 6, 0, 1, 0, 60, 501, 17649);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Eleet', 15222, 18000, 3, 0, 91, 6, 1, 1, 0, 60, 501, 17655);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Filth Flea', 15231, 18000, 29, 0, 130, 6, 0, 1, 0, 60, 501, 17657);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Fresh Clan Functionary', 17905, 18000, 17, 1, 91, 3, 2, 1, 40172, 60, 501, 26147);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Garbage Flea', 15231, 18000, 29, 0, 125, 6, 1, 1, 0, 60, 501, 17657);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Grass Snake', 23353, 18000, 11, 0, 91, 6, 1, 1, 0, 60, 501, 30252);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Gwyndd', 5934, 18000, 0, 1, 110, 2, 3, 1, 300681, 75, 502, 2);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('IIV-X Advanced Docker', 15215, 114400, 0, 0, 150, 6, 1, 1, 0, 60, 502, 17649);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Infected Attendant', 96024, 18000, 16, 0, 96, 6, 0, 1, 0, 60, 501, 96056);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Kneebreaker Alfonzo Rizzolo', 5900, 17920, 23, 1, 120, 4, 2, 1, 40117, 60, 500, 165196);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Leet', 15222, 18000, 1, 0, 90, 6, 1, 1, 0, 60, 501, 17655);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Malfunctioning Cleaning Robot', 297018, 18000, 5, 0, 200, 6, 1, 1, 0, 60, 503, 297023);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Mugger', 17534, 18000, 44, 1, 93, 1, 2, 1, 160561, 60, 501, 203734);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Mutated Garbage Flea', 15231, 70200, 0, 0, 240, 6, 1, 1, 0, 60, 502, 17657);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Nailbomb', 17899, 18000, 0, 1, 90, 4, 1, 1, 40098, 75, 500, 4);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Real Mean Outlaw', 5907, 18000, 5511, 1, 127, 1, 2, 1, 20103, 60, 500, 26092);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Reet of Paradise', 25733, 18000, 5, 0, 90, 6, 1, 1, 0, 60, 501, 30365);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Rollerrat', 15272, 18000, 29, 0, 125, 6, 1, 1, 0, 60, 501, 17687);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Security Camera', 210194, 18000, 6620, 0, 100, 6, 0, 1, 0, 60, 500, 210237);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Shadow', 30434, 10000, 0, 0, 96, 6, 0, 1, 0, 60, 500, 30464);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Slum Runner', 31774, 18000, 66, 0, 96, 6, 0, 1, 0, 60, 501, 55648);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Stim Fiend', 5907, 18000, 72, 1, 96, 1, 2, 1, 40693, 60, 500, 203739);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Supreme Collector of Waste', 17316, 97000, 0, 0, 160, 6, 1, 1, 0, 60, 502, 17714);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Thief', 5907, 18000, 29, 1, 93, 1, 2, 1, 160561, 60, 502, 26092);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Uncontrollable Anger', 96177, 18000, 14, 0, 96, 6, 0, 1, 0, 60, 501, 96195);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Violent Vagabond', 17870, 18000, 21, 1, 93, 1, 2, 1, 40676, 60, 501, 203733);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Waste Collector', 17316, 12200, 11, 0, 75, 6, 1, 1, 0, 60, 500, 17714);
REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes, MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, DeathAnimation, MonsterData) VALUES ('Workman Striker', 17899, 6400, 0, 1, 97, 4, 1, 1, 20007, 60, 500, 203854);

