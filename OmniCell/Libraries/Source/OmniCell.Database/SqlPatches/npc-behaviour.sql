-- How each kind of creature lives, by name, measured from the fifteen 2026-09 retail recordings by
-- scratch LifeAnalysis; see ZoneEngine Core/NpcLife.cs. Player pets are left out.
--
--   WanderShare         of the creatures of that name seen, the share that walked about at all
--   WanderPauseMin/Max  seconds from the start of one wander step to the next (retail p25 and p75)
--   WanderRuns          1 when it ran more steps than it walked
--   Aggressive          1 when it started fights with a player at least half as often as it only
--                       fought back (Garbage Flea 12 against 1; Cleaning Robot 0 against 6)
--   AggroRadius         metres: the retail chase from where it attacked to its first hit (median),
--                       plus 2.5 m of reach; 12 where no chase was measured. 6 to 25.
--
-- npcwanderpoints: every place a creature of that name walked to in retail, 0.5 m grid, per playfield
-- (the recordings' Arete Landing instance 2150461 is written as 6553). A wandering creature only ever
-- walks to one of these, so it stays on ground the live server used.

CREATE TABLE IF NOT EXISTS `npcbehaviour` (`Id` int(32) NOT NULL AUTO_INCREMENT, `NpcName` varchar(255) NOT NULL, `WanderShare` float NOT NULL DEFAULT 0, `WanderPauseMin` float NOT NULL DEFAULT 3, `WanderPauseMax` float NOT NULL DEFAULT 6, `WanderRuns` int(32) NOT NULL DEFAULT 0, `Aggressive` int(32) NOT NULL DEFAULT 0, `AggroRadius` float NOT NULL DEFAULT 12, PRIMARY KEY (`Id`) USING BTREE, UNIQUE KEY `npc` (`NpcName`) USING BTREE) ENGINE=InnoDB DEFAULT CHARSET=utf8;
CREATE TABLE IF NOT EXISTS `npcwanderpoints` (`Id` int(32) NOT NULL AUTO_INCREMENT, `Playfield` int(32) NOT NULL, `NpcName` varchar(255) NOT NULL, `X` float NOT NULL, `Y` float NOT NULL, `Z` float NOT NULL, PRIMARY KEY (`Id`) USING BTREE, KEY `npc` (`Playfield`, `NpcName`) USING BTREE) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DELETE FROM npcbehaviour;
DELETE FROM npcwanderpoints;

-- seen 7, moved 0, unprovoked 1, provoked 1, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Acolyte Kellian', 0, 3, 6, 0, 1, 12);
-- seen 17, moved 2, unprovoked 1, provoked 2, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Acolyte Seleen', 0.12, 0.5, 1.61, 1, 1, 12);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Altumus', 1, 3, 6, 1, 0, 12);
-- seen 13, moved 5, unprovoked 2, provoked 0, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ancient Gargantula', 0.38, 1.47, 2.41, 0, 1, 12);
-- seen 15, moved 8, unprovoked 2, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ancient Nightcrawler', 0.53, 1.42, 2.04, 0, 1, 12);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Anger Manifestation', 1, 1.16, 2.06, 1, 0, 12);
-- seen 4, moved 3, unprovoked 2, provoked 1, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ankari Cornuta', 0.75, 1.33, 8.09, 1, 1, 12);
-- seen 4, moved 4, unprovoked 1, provoked 2, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ankari Oneirodes', 1, 1.03, 1.71, 0, 1, 12);
-- seen 43, moved 29, unprovoked 1, provoked 3, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Architect Striker', 0.67, 61.06, 61.51, 1, 0, 8.18);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Bloodcreeper', 1, 11.35, 39.65, 0, 0, 12);
-- seen 29, moved 29, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Bored Traveller', 1, 1.97, 2.9, 0, 0, 12);
-- seen 6, moved 0, unprovoked 1, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Bruiser', 0, 3, 6, 0, 1, 12);
-- seen 4, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cha Cornuta', 0.5, 1.28, 1.85, 0, 0, 12);
-- seen 6, moved 4, unprovoked 0, provoked 3, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cha Cyathus', 0.67, 1.64, 6.17, 1, 0, 12);
-- seen 10, moved 4, unprovoked 2, provoked 9, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cha Esox', 0.4, 1.41, 13.87, 1, 0, 12);
-- seen 2, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Chiffchaff Reet', 0.5, 0.72, 2.45, 0, 0, 12);
-- seen 61, moved 14, unprovoked 0, provoked 6, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cleaning Robot', 0.23, 0.5, 2.87, 0, 0, 12);
-- seen 4, moved 3, unprovoked 8, provoked 18, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cloaked Kyr''Ozch Ensign', 0.75, 2.95, 33.21, 1, 0, 13.71);
-- seen 3, moved 3, unprovoked 8, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cloaked Kyr''Ozch Specialist', 1, 0.92, 54.2, 1, 1, 12);
-- seen 2, moved 2, unprovoked 1, provoked 16, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cloaked Kyr''Ozch Technician', 1, 3, 6, 1, 0, 12);
-- seen 825, moved 244, unprovoked 77, provoked 45, chases measured 50
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Cultist', 0.3, 1.7, 5.1, 0, 1, 12);
-- seen 6, moved 6, unprovoked 1, provoked 5, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Dasyatis', 1, 0.82, 2.84, 0, 0, 12);
-- seen 32, moved 20, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Deathless Legionnaire', 0.63, 2.59, 5.21, 0, 0, 12);
-- seen 5, moved 3, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Defender of the Three', 0.6, 8.22, 15.97, 0, 0, 12);
-- seen 115, moved 62, unprovoked 0, provoked 7, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Discarded Pet', 0.54, 1.29, 2.99, 0, 0, 12);
-- seen 80, moved 48, unprovoked 0, provoked 9, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Disobedient Bot', 0.6, 1.54, 2.15, 0, 0, 12);
-- seen 2, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Engineer Warbot', 1, 1.15, 39.45, 1, 0, 12);
-- seen 6, moved 2, unprovoked 1, provoked 3, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ensign - Cha''Heru', 0.33, 0.82, 1.53, 1, 0, 17.44);
-- seen 4, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ensign - Cha''Uri', 0.5, 3.1, 5.53, 1, 0, 12);
-- seen 2, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ensign - Jaax''Heru', 1, 3, 6, 1, 0, 12);
-- seen 2, moved 2, unprovoked 0, provoked 1, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ensign - Jaax''Uri', 1, 0.81, 10.88, 1, 0, 12);
-- seen 4, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ensign - Xoch''Heru', 0.5, 3, 6, 1, 0, 12);
-- seen 4, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Ensign - Xoch''Uri', 0.5, 3, 6, 1, 0, 12);
-- seen 6, moved 3, unprovoked 3, provoked 1, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Eternal Sentinel', 0.5, 1.44, 3.66, 1, 1, 12);
-- seen 8, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Farno Kilharha', 0.13, 3, 6, 0, 0, 12);
-- seen 273, moved 111, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Filth Flea', 0.41, 0.5, 1.52, 0, 0, 12);
-- seen 2, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Fresh OT Mercenary', 0.5, 3, 6, 0, 0, 12);
-- seen 20, moved 20, unprovoked 12, provoked 1, chases measured 8
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Garbage Flea', 1, 3.01, 5.27, 0, 1, 9.74);
-- seen 5, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Gargantula', 0.2, 3.12, 3.12, 0, 0, 12);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Gnarl the Roller', 1, 3, 6, 1, 0, 12);
-- seen 266, moved 159, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('ICC Peacekeeper', 0.6, 4.24, 7.98, 0, 0, 12);
-- seen 34, moved 34, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('ICC Peacekeeper Commander', 1, 2.44, 4.34, 0, 0, 12);
-- seen 2, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Incomplete Rebuild', 1, 12.73, 31.2, 0, 0, 12);
-- seen 25, moved 11, unprovoked 0, provoked 2, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Infected Attendant', 0.44, 1.34, 2.29, 0, 0, 12);
-- seen 32, moved 15, unprovoked 3, provoked 1, chases measured 3
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Infector', 0.47, 1.51, 2.77, 0, 1, 18.11);
-- seen 2, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Jaax Cornuta', 1, 3, 6, 1, 0, 12);
-- seen 4, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Jaax Cyathus', 0.5, 1.26, 3.21, 0, 0, 12);
-- seen 16, moved 12, unprovoked 0, provoked 2, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Jaax Esox', 0.75, 0.81, 1.85, 0, 0, 12);
-- seen 5, moved 0, unprovoked 1, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Kneebreaker Alfonzo Rizzolo', 0, 3, 6, 0, 1, 12);
-- seen 33, moved 33, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Leet', 1, 1.12, 1.74, 0, 0, 12);
-- seen 8, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Leetas', 0.25, 1.11, 2.08, 0, 0, 12);
-- seen 5, moved 4, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Levon Karubian', 0.8, 38.25, 38.25, 0, 0, 12);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Lolly the Reet', 1, 2.98, 4.76, 1, 0, 12);
-- seen 18, moved 17, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Maddy Cardile', 0.94, 2.48, 3.16, 0, 0, 12);
-- seen 81, moved 68, unprovoked 0, provoked 3, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Malfunctioning Cleaning Robot', 0.84, 0.5, 0.5, 1, 0, 12);
-- seen 2, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Malikai the Faithful', 1, 5.9, 8.67, 0, 0, 12);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Mario Carles', 1, 7.08, 14.75, 0, 0, 12);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Melissia Toussand', 1, 0.87, 8.86, 1, 0, 12);
-- seen 3, moved 2, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Molested Molecules', 0.67, 1.26, 2.88, 0, 0, 12);
-- seen 41, moved 27, unprovoked 2, provoked 1, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Mugger', 0.66, 0.79, 1.32, 1, 1, 12);
-- seen 1, moved 1, unprovoked 1, provoked 0, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Mutated Garbage Flea', 1, 3, 6, 1, 1, 12);
-- seen 16, moved 9, unprovoked 2, provoked 0, chases measured 2
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Neural Burnout', 0.56, 1.89, 3.11, 0, 1, 16.22);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Nihana Fincher', 1, 3, 6, 0, 0, 12);
-- seen 42, moved 19, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Novice Nanomanipulator', 0.45, 0.87, 2.23, 0, 0, 12);
-- seen 35, moved 8, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Novice Nanoshifter', 0.23, 1.32, 2.26, 0, 0, 12);
-- seen 31, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Omni-AF Urban Trooper', 0.03, 3, 6, 0, 0, 12);
-- seen 14, moved 4, unprovoked 3, provoked 0, chases measured 3
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Oran the Faithful', 0.29, 3, 6, 1, 1, 6);
-- seen 5, moved 3, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Premature Pattern', 0.6, 2.41, 14.22, 0, 0, 12);
-- seen 15, moved 6, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Protester', 0.4, 5.08, 11.8, 0, 0, 12);
-- seen 4, moved 1, unprovoked 2, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Reanimated Corpse', 0.25, 3, 6, 1, 1, 12);
-- seen 12, moved 7, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Reet Warbler', 0.58, 1.17, 2.21, 0, 0, 12);
-- seen 2, moved 1, unprovoked 1, provoked 1, chases measured 1
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Reverend Gulard', 0.5, 10.25, 10.25, 1, 1, 15.52);
-- seen 15, moved 7, unprovoked 1, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Reverend Oluay', 0.47, 1.27, 1.82, 1, 1, 12);
-- seen 10, moved 4, unprovoked 6, provoked 5, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Rimah Esox', 0.4, 1.21, 6.43, 1, 1, 12);
-- seen 1, moved 1, unprovoked 3, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Robotic Guard Dog', 1, 3.78, 114.26, 1, 1, 12);
-- seen 17, moved 3, unprovoked 7, provoked 2, chases measured 5
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Rollerrat', 0.18, 97.39, 97.39, 1, 1, 16.55);
-- seen 62, moved 23, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Rookie Nanorobber', 0.37, 1.02, 2.35, 0, 0, 12);
-- seen 158, moved 37, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Shadow', 0.23, 1.08, 1.35, 0, 0, 12);
-- seen 70, moved 17, unprovoked 12, provoked 5, chases measured 8
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Slum Runner', 0.24, 0.5, 5.37, 0, 1, 10.78);
-- seen 53, moved 25, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Soft Nanohoarder', 0.47, 1.25, 2.13, 0, 0, 12);
-- seen 43, moved 21, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Soft Nanoplaguer', 0.49, 0.81, 2.57, 0, 0, 12);
-- seen 57, moved 23, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Soft Techrejecter', 0.4, 1.1, 2.11, 0, 0, 12);
-- seen 46, moved 19, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Soft Techwrecker', 0.41, 1.31, 2.37, 0, 0, 12);
-- seen 8, moved 5, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Soleet', 0.63, 1.55, 2.13, 0, 0, 12);
-- seen 39, moved 20, unprovoked 0, provoked 5, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Stim Fiend', 0.51, 1.35, 3.57, 0, 0, 12);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Tabitha Ginsberg', 1, 2.76, 6.5, 0, 0, 12);
-- seen 7, moved 6, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Thief', 0.86, 1.57, 7.43, 0, 0, 12);
-- seen 2, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Tiny Stalker', 0.5, 1.79, 2.81, 0, 0, 12);
-- seen 14, moved 8, unprovoked 2, provoked 1, chases measured 2
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Uncontrollable Anger', 0.57, 2.69, 7.86, 0, 1, 14.14);
-- seen 71, moved 8, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Unicorn Guard', 0.11, 5.15, 11.29, 0, 0, 12);
-- seen 101, moved 57, unprovoked 0, provoked 23, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Violent Vagabond', 0.56, 1.43, 2.8, 0, 0, 12);
-- seen 91, moved 34, unprovoked 5, provoked 9, chases measured 5
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Workman Striker', 0.37, 1.63, 3.54, 0, 1, 6.7);
-- seen 1, moved 1, unprovoked 0, provoked 0, chases measured 0
INSERT INTO npcbehaviour (NpcName, WanderShare, WanderPauseMin, WanderPauseMax, WanderRuns, Aggressive, AggroRadius) VALUES ('Zhok the Abomination', 1, 0.54, 1.03, 1, 0, 12);

INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3589, 41.5, 831.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3593, 42, 833.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3593.5, 42, 831);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3597.5, 42, 830);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3600.5, 42, 834);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3603.5, 41.5, 831.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3607, 41.5, 830);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3612, 41.5, 831.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3612.5, 41.5, 829);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3613.5, 41.5, 832.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3621.5, 41.5, 832.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3622, 41.5, 827.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3626.5, 41.5, 830.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3629.5, 41.5, 822);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3630, 48.5, 799.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3630, 41.5, 829);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3630.5, 50, 794.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3632, 41.5, 829);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3633, 46, 807);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3633.5, 42.5, 817);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3634.5, 41.5, 833);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3635.5, 41.5, 830);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3636, 41, 834.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3636.5, 41.5, 825);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3636.5, 41.5, 830);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Anger Manifestation', 3637.5, 41.5, 836);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3589, 25, 882);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3589.5, 24, 885.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3589.5, 23, 890);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3590, 20.5, 896);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3590, 18.5, 901.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3590.5, 25.5, 880);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3592, 25, 883);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3592, 23, 888.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3593, 25.5, 876.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3593, 25.5, 880);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3593, 20.5, 896.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3593.5, 52.5, 800);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3593.5, 18.5, 902);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3594, 52.5, 799.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3594, 52.5, 800);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3594.5, 52.5, 800);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3597.5, 25.5, 875.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3598, 52.5, 781.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3598, 52.5, 782);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3598, 25.5, 880.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3598.5, 52.5, 782);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3599.5, 52.5, 795);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3600, 52.5, 794.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3600, 52.5, 795);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3600.5, 52.5, 795);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3600.5, 25.5, 876.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3602, 52.5, 787.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3602, 52.5, 788);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3602, 52.5, 788.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3602.5, 52.5, 788);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3602.5, 25.5, 880.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3603, 52.5, 775);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3603, 52.5, 775.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3603.5, 52.5, 775);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3603.5, 26, 879);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3604.5, 26.5, 879);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3607.5, 27.5, 876.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3609.5, 52.5, 798);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3609.5, 28, 878.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3610, 52.5, 797.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3610, 52.5, 798);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3610, 52.5, 798.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3613, 52.5, 783);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3613, 52.5, 783.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3613.5, 52.5, 783);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3613.5, 31, 877.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3614, 29.5, 875.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3614.5, 52.5, 796);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3615, 52.5, 795.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3615, 52.5, 796);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3615.5, 52.5, 796);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3615.5, 30.5, 879);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3619, 31, 878.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3619.5, 39, 840.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3620, 40, 837.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3620, 38, 844.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3620.5, 41, 830);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3621.5, 52.5, 798);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3621.5, 33.5, 877.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3622, 52.5, 797.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3622, 52.5, 798);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3622, 52.5, 798.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3622.5, 52.5, 797);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3622.5, 40.5, 836);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623, 52.5, 796.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623, 52.5, 797);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623, 52.5, 797.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623, 38, 843.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623.5, 41, 826.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623.5, 40.5, 835.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623.5, 39.5, 839.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3623.5, 40, 840.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3625, 41, 823);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3626.5, 41, 834);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3630, 41, 837);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3631, 41, 820.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3633, 41, 831.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3633, 41, 841);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3635.5, 41, 829.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3637, 41, 834.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3637, 41, 841.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3639, 41, 838);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3639.5, 42.5, 841);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Cleaning Robot', 3641.5, 41, 824.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3419.5, 0, 872);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3421.5, 0, 802.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3421.5, 0, 871);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3421.5, 0, 873.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422, 0.5, 865);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422, 0.5, 865.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422, 0, 873.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422, 0, 891);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0, 801.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0, 821.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0, 865);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0.5, 865);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0.5, 865.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0, 869.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0, 872);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3422.5, 0, 875.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3424, 0.5, 819);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3424, 0.5, 819.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3424, 0, 856);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3424, 0, 863);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3424, 0, 870);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3425, 0, 869);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3425.5, 0, 867);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3426, 0, 860.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3426, 0, 889);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3426.5, 0, 806.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3426.5, 0, 810.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3426.5, 0, 854);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3426.5, 0, 882);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3427, 0, 830);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3429.5, 0, 865.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3430, 0, 806);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3436, 0, 801.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3437.5, 0.5, 803.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3439.5, 0.5, 801);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3439.5, 0.5, 801.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3440, 0.5, 801.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3441, 0, 806);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3441.5, 0, 801);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3446.5, 0, 801);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3448.5, 0, 806);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3448.5, 0.5, 895);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3449, 0, 802.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3449, 0.5, 813);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3449, 0.5, 827);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3449, 0.5, 833);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3450, 0.5, 890);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3450.5, 1, 817);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3450.5, 1, 862);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3451, 1, 858);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3451.5, 0.5, 812.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3451.5, 1, 852.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3452, 1.5, 865);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3453, 0.5, 886);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3453.5, 0.5, 808);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3453.5, 0.5, 879.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3454, 1, 846.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3454, 1, 874);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3454.5, 1, 841.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3462, 9.5, 814);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3501.5, 6, 908);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3502.5, 5, 889);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3502.5, 5, 903.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3503, 5, 896);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3514, 8, 931.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3518.5, 5, 890);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3520.5, 7, 927);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3522.5, 9, 956);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3523, 5, 888);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3524.5, 7, 930);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3529, 6.5, 932.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3529.5, 5, 890.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3530.5, 5, 897.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3531, 5.5, 902);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3532.5, 8, 907.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3537.5, 8.5, 950.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3541, 8.5, 888);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3541.5, 7, 895.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3541.5, 8.5, 930);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3545, 7.5, 888);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3546, 9.5, 944);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3547, 8, 927);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3549, 7, 898);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3549.5, 6, 885.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3549.5, 9.5, 938);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3550.5, 7.5, 887);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3552.5, 5, 868);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3552.5, 10.5, 936);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3553, 8.5, 887.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3553, 7.5, 921.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3554.5, 5, 874);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3555.5, 8, 902);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3555.5, 8, 906.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3555.5, 8, 910.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3556.5, 9, 923.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3557, 5, 865.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3558, 5.5, 877);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3558.5, 8, 916);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3562, 5, 865.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3565.5, 5.5, 862.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Garbage Flea', 3567.5, 8.5, 913);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Gnarl the Roller', 3386, 4.5, 712.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3378, 17, 812.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3378, 17, 814.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3378.5, 17, 808);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3379, 17, 802.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3382, 15, 793.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3382, 15, 800.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3383.5, 16, 810);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3384, 16, 801);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3384, 16, 806);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3386, 15.5, 803.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3386.5, 15.5, 802);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3390, 14, 801.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3390, 13.5, 803.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3392.5, 12.5, 802);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3393.5, 12, 804);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3396.5, 11, 805.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3399.5, 9.5, 801);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3400, 9, 806);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3403.5, 9, 803);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3405, 4.5, 768);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3405.5, 9, 806.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3410, 4.5, 779);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3410, 9, 802.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3410.5, 3.5, 774);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3413, 5, 782);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3414.5, 9, 807.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3415.5, 3, 769);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3417, 5, 782);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3421.5, 5, 781);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3422.5, 5, 781);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3423, 3, 767);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3424, 9, 906.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3425, 9, 915);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3426, 9, 894.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3426.5, 9, 807);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3426.5, 9, 858.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3427, 5, 781);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3427, 9, 820);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3427, 9, 831.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3427, 9, 844);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3427, 9, 888.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3430.5, 3, 768);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3431.5, 9, 899);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3435.5, 5, 781);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3437, 9, 910);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3438, 3, 772);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3438.5, 9, 902);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3439.5, 5, 781);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3442, 9, 805.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3442.5, 4, 774);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3442.5, 11, 901.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3443, 12.5, 901.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3445, 9, 912);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3446, 5, 780);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3446.5, 5, 777);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3449.5, 9, 806.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3450, 9, 914.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3453.5, 5, 782);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3453.5, 5, 786);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3454, 9, 870);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3454.5, 9, 812);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3454.5, 9, 891.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3455.5, 9, 911);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3456.5, 5, 786.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3456.5, 9, 840);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3456.5, 9, 902.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3460.5, 4.5, 782);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3464, 8, 787);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3465, 6, 782.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3468, 7, 781);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3473, 8, 786.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3476.5, 6, 782);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3479, 8.5, 786);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3483, 5, 782.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3484, 6.5, 787);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3487, 5, 786);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'ICC Peacekeeper', 3487.5, 5, 783);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3308.5, 1, 718);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3325, 2.5, 718.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3347, 8.5, 707.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3353, 6.5, 707.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3357, 3, 667.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3358, 3.5, 640.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3358.5, 3, 690);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3360, 3.5, 620.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3365.5, 2, 607);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3380, 2, 586.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Lolly the Reet', 3389, 2, 574.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3593.5, 52.5, 800);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3594, 52.5, 799.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3594, 52.5, 800);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3594, 52.5, 800.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3594.5, 52.5, 800);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3596, 52, 784);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3596, 51.5, 784);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3596.5, 52.5, 772);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3597, 52.5, 771.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3597, 52.5, 772);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3597, 52.5, 772.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3597, 52, 772.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3597.5, 52.5, 772);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3598.5, 52.5, 787);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3599, 52.5, 786.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3599, 52.5, 787);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3599, 52.5, 787.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3599.5, 52.5, 787);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3601.5, 52.5, 788);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3602, 52.5, 787.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3602, 52.5, 788);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3602, 52.5, 788.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3602.5, 52.5, 788);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3610.5, 52, 778);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3610.5, 52.5, 778);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3611, 52.5, 778);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3611, 52, 778);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3611, 52.5, 778.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3611.5, 52.5, 778);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3611.5, 52, 778);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3612, 52.5, 787.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3612, 52.5, 788);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3612, 52.5, 788.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3612.5, 52.5, 788);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3620.5, 52.5, 784);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3621, 52.5, 784);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3621, 52.5, 784.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3621.5, 52.5, 784);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3622.5, 52.5, 799);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3623, 52.5, 798.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3623, 52.5, 799);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3623, 52.5, 799.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Malfunctioning Cleaning Robot', 3623.5, 52.5, 799);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3378.5, 3, 720);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3385, 3, 729);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3397.5, 3, 739);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3403, 2, 694.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3413, 3, 755.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3435, 3, 760.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3455.5, 4.5, 761.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3463.5, 3.5, 761.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3591, 2.5, 718);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3596, 3, 728);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3600, 3, 736);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3611.5, 3, 731.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3619.5, 2.5, 734);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mario Carles', 3631, 2, 734.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Mutated Garbage Flea', 3423.5, 0.5, 888);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3508.5, 5, 793.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3509, 5, 786.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3512.5, 5, 794.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3517.5, 5, 797.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3521, 6.5, 780.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3526.5, 5, 798.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3533, 5, 802);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3533, 5, 811.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3533.5, 5, 818.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3534.5, 6.5, 781.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3536, 5, 800);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3538, 5, 786.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3540, 5, 796);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3541, 5, 792.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3543, 7, 778.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3543, 5, 824);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3549.5, 5, 824);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3550, 7, 778.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3559, 7, 779);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3559.5, 5, 801);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3563, 5, 799);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3564, 5, 808.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3566.5, 5, 816.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3567, 7, 777);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3567.5, 5, 825);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3573.5, 5, 801.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3578.5, 7, 769.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3586.5, 7, 770.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3592, 5, 801.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3593, 6, 771);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Protester', 3598, 5, 795.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Robotic Guard Dog', 3419, 9.5, 857.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Robotic Guard Dog', 3423.5, 9.5, 838.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Robotic Guard Dog', 3428.5, 9.5, 879.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Robotic Guard Dog', 3441, 9, 891.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Rollerrat', 3380.5, 2.5, 663);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Rollerrat', 3386, 3.5, 743);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Rollerrat', 3387, 3.5, 743.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Rollerrat', 3388, 3.5, 743);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3410.5, 9, 853.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3411.5, 9, 857.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3412.5, 9, 860.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3413.5, 9, 849);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3413.5, 9, 870.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3414.5, 9, 875.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3416.5, 9, 844);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3417.5, 9, 834);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3421.5, 9, 835);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3421.5, 9, 867.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3421.5, 9, 878);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3422.5, 9, 839.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3422.5, 11, 846.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3422.5, 9, 881.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3425, 9, 882.5);
INSERT INTO npcwanderpoints (Playfield, NpcName, X, Y, Z) VALUES (6553, 'Tabitha Ginsberg', 3425.5, 9, 874);
