-- What each kind of creature looks like beyond its MonsterData and scale, by name: the textures
-- laid over its model (SimpleCharFullUpdate extended textures, also its corpse's meshes), the second
-- monster scale and the glow of a dynamic creature: the nanos it shows from the start, kept only where
-- every one of them runs 10000/10000 (buffs on a player's pet count down and are left out). The most common
-- form of every NPC update in the fifteen 2026-09 retail recordings, read by scratch MsgDump; see
-- ZoneEngine Core/NpcAppearance.cs. Creatures with none of the three are not listed.

CREATE TABLE IF NOT EXISTS `npcappearance` (`Id` int(32) NOT NULL AUTO_INCREMENT, `NpcName` varchar(255) NOT NULL, `SecondMonsterScale` int(32) NOT NULL DEFAULT -1, PRIMARY KEY (`Id`) USING BTREE, UNIQUE KEY `npc` (`NpcName`) USING BTREE) ENGINE=InnoDB DEFAULT CHARSET=utf8;
CREATE TABLE IF NOT EXISTS `npctextures` (`Id` int(32) NOT NULL AUTO_INCREMENT, `NpcName` varchar(255) NOT NULL, `Ordinal` int(32) NOT NULL DEFAULT 0, `MaterialName` varchar(32) NOT NULL DEFAULT '', `TextureId` int(32) NOT NULL DEFAULT 0, `OverlayId` int(32) NOT NULL DEFAULT 0, `AlphaMode` int(32) NOT NULL DEFAULT 0, PRIMARY KEY (`Id`) USING BTREE, UNIQUE KEY `npcordinal` (`NpcName`, `Ordinal`) USING BTREE) ENGINE=InnoDB DEFAULT CHARSET=utf8;
CREATE TABLE IF NOT EXISTS `npcactivenanos` (`Id` int(32) NOT NULL AUTO_INCREMENT, `NpcName` varchar(255) NOT NULL, `Ordinal` int(32) NOT NULL DEFAULT 0, `NanoType` int(32) NOT NULL DEFAULT 53019, `NanoId` int(32) NOT NULL DEFAULT 0, `Time1` int(32) NOT NULL DEFAULT 0, `Time2` int(32) NOT NULL DEFAULT 0, PRIMARY KEY (`Id`) USING BTREE, UNIQUE KEY `npcordinal` (`NpcName`, `Ordinal`) USING BTREE) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DELETE FROM npcappearance;
DELETE FROM npctextures;
DELETE FROM npcactivenanos;

-- Yakushi Nyorai  (seen 3)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Yakushi Nyorai', 0, 'metapet_healing', 288733, 0, 1);
-- Fumizu  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Fumizu', 0, 'metapet_stun', 224234, 0, 0);
-- Mendy  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mendy', 0, 'metapet_healing', 288730, 0, 1);
-- SLO-MO  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('SLO-MO', 0, 'metapet_healing', 288730, 0, 1);
-- Shifu Yan Lei  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Shifu Yan Lei', 0, 'Swampy_tpage', 209432, 0, 1);
-- Asclepius  (seen 3)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Asclepius', 0, 'metapet_healing', 288733, 0, 1);
-- 32-V Docker  (seen 141)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('32-V Docker', 0, 'Material #1', 31825, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('32-V Docker', 1, 'Material #3', 31824, 0, 0);
-- Aaradon  (seen 2)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Aaradon', 12);
-- Altumus  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Altumus', 0, 'metapet_healing', 288730, 0, 1);
-- Asyncgon  (seen 2)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Asyncgon', 176);
-- Asyncheals  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Asyncheals', 12);
-- Automated Agency Receptionist  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Automated Agency Receptionist', 0, 'Material #1', 283781, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Automated Agency Receptionist', 1, 'Material #3', 283780, 0, 0);
-- Belamorte  (seen 50)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Belamorte', 0, 'metapet_healing', 288730, 0, 1);
-- Blunt007  (seen 4)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Blunt007', 12);
-- BoltZ  (seen 8)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('BoltZ', 206);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('BoltZ', 0, 'Material #1', 275990, 0, 1);
-- Bureaucrat Assistant  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Bureaucrat Assistant', 0, 'Material #468', 96035, 0, 0);
-- Bureaucrat Helper  (seen 18)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Bureaucrat Helper', 0, 'Material #468', 96040, 0, 0);
-- Burning Cleaning Robot  (seen 39)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Burning Cleaning Robot', 0, 'mdrone1', 297282, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Burning Cleaning Robot', 1, 'mdrone2', 297283, 0, 0);
-- CEO Guardian  (seen 5)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('CEO Guardian', 0, 'hellface2', 224341, 0, 1);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('CEO Guardian', 1, 'hell2', 224342, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('CEO Guardian', 2, 'hell1', 224342, 0, 0);
-- Chicago  (seen 10)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Chicago', 12);
-- Chuelove  (seen 3)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Chuelove', 12);
-- City Administrator - Rex Chapman  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('City Administrator - Rex Chapman', 0, 'feet', 246866, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('City Administrator - Rex Chapman', 1, 'body', 247096, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('City Administrator - Rex Chapman', 2, 'arms', 246864, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('City Administrator - Rex Chapman', 3, 'legs', 246868, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('City Administrator - Rex Chapman', 4, 'hands', 246867, 0, 0);
-- Cleaning Robot  (seen 225)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Cleaning Robot', 0, 'Material #1', 295519, 0, 0);
-- Cleanmeister Intelligence Robot  (seen 11)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Cleanmeister Intelligence Robot', 40);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Cleanmeister Intelligence Robot', 0, 'mdrone1', 96068, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Cleanmeister Intelligence Robot', 1, 'mdrone2', 96069, 0, 0);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Cleanmeister Intelligence Robot', 0, 53019, 162268, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Cleanmeister Intelligence Robot', 1, 53019, 160070, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Cleanmeister Intelligence Robot', 2, 53019, 160085, 10000, 10000);
-- Dahbihgah  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Dahbihgah', 7);
-- Darthcadeus  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Darthcadeus', 12);
-- Deathless Legionnaire  (seen 32)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Deathless Legionnaire', 0, 'skull', 95980, 0, 0);
-- Des Morck  (seen 8)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Des Morck', 0, 'legs', 8965, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Des Morck', 1, 'feet', 8960, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Des Morck', 2, 'arms', 8964, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Des Morck', 3, 'hands', 8963, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Des Morck', 4, 'body', 8961, 0, 0);
-- Desert Reet  (seen 52)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Desert Reet', 0, 'cute_birdy', 95858, 0, 0);
-- Dr.Grimm  (seen 18)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Dr.Grimm', 0, 'metapet_healing', 288730, 0, 1);
-- DrJenkill  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('DrJenkill', 0, 'metapet_healing', 288730, 0, 1);
-- Elfcratt  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Elfcratt', 12);
-- Elfkeeper  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Elfkeeper', 12);
-- Ettu the Cursed  (seen 45)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Ettu the Cursed', 0, 'Swampy_tpage', 209431, 0, 1);
-- Farno Kilharha  (seen 9)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Farno Kilharha', 0, 'arms', 8714, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Farno Kilharha', 1, 'feet', 8710, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Farno Kilharha', 2, 'hands', 8712, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Farno Kilharha', 3, 'legs', 8713, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Farno Kilharha', 4, 'body', 8709, 0, 0);
-- Feely  (seen 36)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Feely', 0, 'metapet_healing', 288730, 0, 1);
-- Filth Flea  (seen 1387)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Filth Flea', 0, 'Material #9', 15233, 0, 1);
-- Fixyami  (seen 5)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Fixyami', 5);
-- Flea  (seen 4)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Flea', 0, 'Material #9', 15233, 0, 1);
-- Focalin  (seen 4)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Focalin', 12);
-- Furry Git  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Furry Git', 0, 'Material #1', 275990, 0, 1);
-- Garbage Flea  (seen 230)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Garbage Flea', 0, 'Material #9', 95883, 0, 1);
-- Gianna Molla  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Gianna Molla', 0, 'legs', 8884, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Gianna Molla', 1, 'hands', 8887, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Gianna Molla', 2, 'body', 8883, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Gianna Molla', 3, 'feet', 8886, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Gianna Molla', 4, 'arms', 8885, 0, 0);
-- Gnarl the Roller  (seen 5)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Gnarl the Roller', 40);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Gnarl the Roller', 0, 'Material #1', 95949, 0, 0);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Gnarl the Roller', 0, 53019, 162267, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Gnarl the Roller', 1, 53019, 160070, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Gnarl the Roller', 2, 53019, 160082, 10000, 10000);
-- Greedy Desert Reet  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Greedy Desert Reet', 0, 'cute_birdy', 95859, 0, 0);
-- ICC Juggernaut  (seen 4)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('ICC Juggernaut', 0, 'jugger2', 297347, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('ICC Juggernaut', 1, 'jugger3', 297348, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('ICC Juggernaut', 2, 'jugger1', 297346, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('ICC Juggernaut', 3, 'jugger4', 297349, 0, 0);
-- IIV-X Advanced Docker  (seen 3)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('IIV-X Advanced Docker', 40);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('IIV-X Advanced Docker', 0, 'Material #1', 40851, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('IIV-X Advanced Docker', 1, 'Material #3', 40852, 0, 0);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('IIV-X Advanced Docker', 0, 53019, 162265, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('IIV-X Advanced Docker', 1, 53019, 160070, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('IIV-X Advanced Docker', 2, 53019, 160083, 10000, 10000);
-- Ina Charlotta Kern  (seen 4)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Ina Charlotta Kern', 0, 'feet', 8850, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Ina Charlotta Kern', 1, 'body', 8848, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Ina Charlotta Kern', 2, 'legs', 8853, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Ina Charlotta Kern', 3, 'arms', 8849, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Ina Charlotta Kern', 4, 'hands', 8852, 0, 0);
-- Inatacrat  (seen 5)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Inatacrat', 12);
-- Jeepz  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Jeepz', 12);
-- Keepinbtz  (seen 2)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Keepinbtz', 12);
-- Kickkat1  (seen 2)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Kickkat1', 12);
-- Kushmon  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Kushmon', 12);
-- Lenus  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Lenus', 12);
-- Litmp's Healer  (seen 9)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Litmp''s Healer', 0, 'metapet_healing', 288730, 0, 1);
-- Lolly the Reet  (seen 3)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Lolly the Reet', 0, 'cute_birdy', 95855, 0, 0);
-- Ltelement  (seen 11)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Ltelement', 12);
-- Malfunctioning Cleaning Robot  (seen 118)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Malfunctioning Cleaning Robot', 0, 'mdrone1', 297282, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Malfunctioning Cleaning Robot', 1, 'mdrone2', 297283, 0, 0);
-- Medinos  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Medinos', 0, 'metapet_healing', 288730, 0, 1);
-- Mick Foley  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mick Foley', 0, 'metapet_healing', 288737, 0, 1);
-- Mortificant the Eternal  (seen 12)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mortificant the Eternal', 0, 'metapet_healing', 288743, 0, 1);
-- Mprave  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Mprave', 12);
-- Mr. Blake  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mr. Blake', 0, 'arms', 8714, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mr. Blake', 1, 'feet', 8710, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mr. Blake', 2, 'hands', 8712, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mr. Blake', 3, 'legs', 8713, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mr. Blake', 4, 'body', 8709, 0, 0);
-- Mutated Garbage Flea  (seen 14)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Mutated Garbage Flea', 40);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Mutated Garbage Flea', 0, 'Material #9', 275711, 0, 1);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Mutated Garbage Flea', 0, 53019, 162265, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Mutated Garbage Flea', 1, 53019, 160070, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Mutated Garbage Flea', 2, 53019, 160083, 10000, 10000);
-- Nekremis  (seen 2)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Nekremis', 7);
-- Nihana Fincher  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Nihana Fincher', 0, 'legs', 8965, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Nihana Fincher', 1, 'feet', 8960, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Nihana Fincher', 2, 'arms', 8964, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Nihana Fincher', 3, 'hands', 8963, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Nihana Fincher', 4, 'body', 8961, 0, 0);
-- Nono  (seen 3)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Nono', 0, 'metapet_healing', 288730, 0, 1);
-- NuttZ  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('NuttZ', 206);
-- Owencreds  (seen 2)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Owencreds', 12);
-- Peacekeeper Constad  (seen 8)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Peacekeeper Constad', 0, 'Material #468', 272342, 0, 0);
-- Randy Savage  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Randy Savage', 0, 'Swampy_tpage', 209432, 0, 1);
-- Ravening M-60  (seen 6)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Ravening M-60', 0, 'Material #1', 275990, 0, 1);
-- Redhotdabs  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Redhotdabs', 12);
-- Researcher - Miranda Prue  (seen 9)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Researcher - Miranda Prue', 0, 'legs', 8884, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Researcher - Miranda Prue', 1, 'hands', 8887, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Researcher - Miranda Prue', 2, 'body', 8883, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Researcher - Miranda Prue', 3, 'feet', 8886, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Researcher - Miranda Prue', 4, 'arms', 8885, 0, 0);
-- Restite  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Restite', 0, 'metapet_healing', 288730, 0, 1);
-- Roboelf  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Roboelf', 12);
-- Robotic Guard Dog  (seen 17)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Robotic Guard Dog', 0, 'Material #9', 95884, 0, 0);
-- Rollerrat  (seen 55)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Rollerrat', 0, 'Material #1', 39966, 0, 0);
-- Rusty Git  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Rusty Git', 206);
-- Saltworm  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Saltworm', 40);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Saltworm', 0, 'Material #3', 95955, 0, 0);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Saltworm', 0, 53019, 162268, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Saltworm', 1, 53019, 160070, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Saltworm', 2, 53019, 160085, 10000, 10000);
-- Salvinous  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Salvinous', 0, 'metapet_healing', 288730, 0, 1);
-- Sanoo  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Sanoo', 0, 'metapet_healing', 288730, 0, 1);
-- Santaelf  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Santaelf', 12);
-- Slayerdroid Annihilator  (seen 87)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Slayerdroid Annihilator', 0, 'slayerdroid', 220319, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Slayerdroid Annihilator', 1, 'slayerdroid2', 220320, 0, 0);
-- Spencer Moss  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Spencer Moss', 0, 'arms', 8714, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Spencer Moss', 1, 'body', 8709, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Spencer Moss', 2, 'feet', 8710, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Spencer Moss', 3, 'hands', 8712, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Spencer Moss', 4, 'legs', 8713, 0, 0);
-- Supreme Collector of Waste  (seen 5)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Supreme Collector of Waste', 40);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Supreme Collector of Waste', 0, 'Material #22', 95885, 0, 1);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Supreme Collector of Waste', 0, 53019, 160070, 10000, 10000);
INSERT INTO npcactivenanos (NpcName, Ordinal, NanoType, NanoId, Time1, Time2) VALUES ('Supreme Collector of Waste', 1, 53019, 160077, 10000, 10000);
-- Surveillance Droid  (seen 11)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Surveillance Droid', 0, 'camera', 210203, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Surveillance Droid', 1, 'camera glow', 239782, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Surveillance Droid', 2, 'camera lense', 239784, 0, 0);
-- Sydney Finn  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Sydney Finn', 0, 'legs', 9027, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Sydney Finn', 1, 'hands', 9023, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Sydney Finn', 2, 'arms', 9028, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Sydney Finn', 3, 'body', 9026, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Sydney Finn', 4, 'feet', 9024, 0, 0);
-- The Rihwen  (seen 11)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('The Rihwen', 0, 'main tpage', 254840, 0, 1);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('The Rihwen', 1, 'armor', 254839, 0, 1);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('The Rihwen', 2, 'shag', 209299, 0, 0);
-- Themercator  (seen 1)
INSERT INTO npcappearance (NpcName, SecondMonsterScale) VALUES ('Themercator', 12);
-- Unicorn Bursar  (seen 9)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Bursar', 0, 'feet', 246866, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Bursar', 1, 'body', 246865, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Bursar', 2, 'arms', 246864, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Bursar', 3, 'legs', 246868, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Bursar', 4, 'hands', 246867, 0, 0);
-- Unicorn Commander  (seen 21)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Commander', 0, 'feet', 246866, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Commander', 1, 'body', 246865, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Commander', 2, 'arms', 246864, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Commander', 3, 'legs', 246868, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Commander', 4, 'hands', 246867, 0, 0);
-- Unicorn Duty Sergeant  (seen 5)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Duty Sergeant', 0, 'feet', 246866, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Duty Sergeant', 1, 'body', 246865, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Duty Sergeant', 2, 'arms', 246864, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Duty Sergeant', 3, 'legs', 246868, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Duty Sergeant', 4, 'hands', 246867, 0, 0);
-- Unicorn Security Administrator  (seen 11)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Security Administrator', 0, 'feet', 246866, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Security Administrator', 1, 'body', 246865, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Security Administrator', 2, 'arms', 246864, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Security Administrator', 3, 'legs', 246868, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Unicorn Security Administrator', 4, 'hands', 246867, 0, 0);
-- Valentyia  (seen 4)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Valentyia', 0, 'metapet_healing', 288731, 0, 1);
-- Waste Collector  (seen 173)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Waste Collector', 0, 'Material #22', 17318, 0, 1);
-- Willox  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Willox', 0, 'metapet_healing', 288732, 0, 1);
-- Yidira  (seen 11)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Yidira', 0, 'metapet_stun', 224234, 0, 0);
-- Zhok the Abomination  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zhok the Abomination', 0, 'Swampy_tpage', 209432, 0, 1);
-- Zix  (seen 2)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zix', 0, 'legss', 247595, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zix', 1, 'bodyss', 247594, 0, 1);
-- Zyvania Bagh  (seen 23)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zyvania Bagh', 0, 'legs', 9027, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zyvania Bagh', 1, 'hands', 9023, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zyvania Bagh', 2, 'arms', 9028, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zyvania Bagh', 3, 'body', 9026, 0, 0);
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('Zyvania Bagh', 4, 'feet', 9024, 0, 0);
-- brb  (seen 10)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('brb', 0, 'cutecreature', 247809, 0, 0);
-- conr  (seen 1)
INSERT INTO npctextures (NpcName, Ordinal, MaterialName, TextureId, OverlayId, AlphaMode) VALUES ('conr', 0, 'metapet_healing', 288730, 0, 1);
