-- Arete Landing, the Shade branch: Lady Sheila Black's chain, from the retail captures
-- 20260914-220505 (stream s5) and 20260915-042412. Generated stages reviewed and trimmed by hand:
-- QuestExtract writes a whole playfield, and this adds only what the Shade sessions showed on top of
-- arete-landing-quests.sql, which stays as it is.
--
-- The chain forks at "Return to Vernon Godfray". He sends most people to Dr. Mason to be fitted with
-- an implant; a Shade he sends to Lady Sheila Black, because "A normal doctor just won't be able to
-- help you out" (s5 #24196). The Shade session was granted no Dr. Mason stage at all and the implant
-- session never met Sheila Black, so RequiresProfession keeps each branch to its own: 15 is Shade
-- only, -15 is everyone except a Shade.

-- The column the fork needs, added once. SqlTables/quests.sql has it for a fresh database.
SET @haveColumn := (
  SELECT COUNT(*) FROM information_schema.columns
  WHERE table_schema = DATABASE() AND table_name = 'quests' AND column_name = 'RequiresProfession');
SET @addColumn := IF(
  @haveColumn = 0,
  'ALTER TABLE quests ADD COLUMN `RequiresProfession` int(32) NOT NULL DEFAULT 0',
  'DO 0');
PREPARE addColumn FROM @addColumn;
EXECUTE addColumn;
DEALLOCATE PREPARE addColumn;

DELETE FROM questtransitions WHERE FromQuest IN (1440331282, 1440331284, 1440331285, 1440331288) OR ToQuest IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM questobjectives WHERE QuestId IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM questitemrewards WHERE QuestId IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM questwire WHERE QuestId IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM questwireactions WHERE QuestId IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM questwirerewards WHERE QuestId IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM charactersquests WHERE QuestId IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM quests WHERE Id IN (1440331282, 1440331284, 1440331285, 1440331288);
DELETE FROM knubotdialogue WHERE Playfield = 6553 AND NpcName = 'Lady Sheila Black';
DELETE FROM knubotopeners WHERE Playfield = 6553 AND NpcName = 'Lady Sheila Black';

-- Talk to Lady Sheila Black  [20260914-220505_s5/s5:1440331282]
INSERT INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires, RequiresProfession) VALUES (1440331282, 'Talk to Lady Sheila Black', 'Talk to Lady Sheila Black<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>After helping Vernon, he gave you a Blank ICC ID Chip. You need to imprint your DNA in to the chip, but due to your special physiology, a normal doctor just wouldn\'t be able to help you. He said that Lady Sheila Black, a Shade might be able to help you out.', 2052536072, 244818, 0, 0, 6553, 0, 15);
-- Finished by DialogueAnswer Lady Sheila Black: "I need my DNA imprented in this Blank ICC ID Chip.."  [20260914-220505_s5/s5 #25414]
INSERT INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, TargetLowId, TargetHighId, TargetQuality, Required) VALUES (1440331282, 0, 12, 'I need my DNA imprented in this Blank ICC ID Chip..', 0, 0, 0, 1);
INSERT INTO questtransitions (FromQuest, ToQuest, Ordinal) VALUES (1440331282, 1440331284, 0);
INSERT INTO questwire (QuestId, Source, GiverType, GiverInstance, QuestCode, UnknownHash, Quality, TimeLimit, Unknown20, Unknown21, Unknown22, Unknown23Type, Unknown23Instance, Unknown25, Unknown26) VALUES (1440331282, 'Captured', 50000, 2052536072, 1095714628, 0, 0, 0, 6, 0, 105159, 0, 0, 0, 7);
INSERT INTO questwireactions (QuestId, Ordinal, Version, ActionType, ActionInstance, Unknown1Type, Unknown1Instance, Unknown2Type, Unknown2Instance, Unknown3Type, Unknown3Instance, Unknown4Type, Unknown4Instance, Unknown5, Unknown6, Unknown7, Unknown8, Unknown9Type, Unknown9Instance, Unknown10, Unknown11, Unknown12, Unknown13, Unknown14Type, Unknown14Instance, Deadline, Unknown16, TrackingType, PlayfieldType, PlayfieldInstance, Unknown18, Unknown19, X, Y, Z) VALUES (1440331282, 0, 24, 0, 0, 0, 0, 70099, 105159, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 54001, 40016, 6553, 100000, 100000, 3413, 0, 904);

-- Give a Soul Capsule to Lady Sheila Black  [20260914-220505_s5/s5:1440331284]
-- The Spirit Siphon (297333) is handed over on accepting, as the capture shows; the Soul Capsule the
-- stage asks for (236635/238946) has no captured drop, so nothing spawns one yet - OmniCell-defined
-- gap, noted in Documentation/Implant-Milestones.md.
INSERT INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires, RequiresProfession) VALUES (1440331284, 'Give a Soul Capsule to Lady ...', 'Give a Soul Capsule to Lady Sheila Black<BR><BR>Lady Sheila Black has given you a <a href=\'itemref://297333/297333/1\'>Nano Crystal (Spirit Siphon)</a>. She wants you to upload this nano program and cast it on a dying monster in an attempt to extract its spirit. Return to her with the Soul Capsule, when you completed this task.<BR><BR><font color="#FF0000">Mission Objective:<BR>Upload the <a href=\'itemref://297333/297333/1\'>Nano Crystal (Spirit Siphon)</a>.<BR>Find a monster to fight, get it below 20% health, then use the <a href=\'itemid://53019/297342\'>Spirit Siphon</a> nano on it.<BR>Make sure you loot the <a href=\'itemref://236635/238946/10\'>Soul Capsule</a> and give it to Lady Sheila Black.</font>', 2052536072, 244818, 0, 0, 6553, 0, 15);
-- Finished by TradeHandIn item 297333 QL1 (linked in the stage description) Lady Sheila Black  [20260914-220505_s5/s5 #26589]
INSERT INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, TargetLowId, TargetHighId, TargetQuality, Required) VALUES (1440331284, 0, 7, '297333', 297333, 297333, 1, 1);
INSERT INTO questitemrewards (QuestId, ItemId, Quantity, GrantOnAccept) VALUES (1440331284, 297333, 1, 1);
INSERT INTO questitemrewards (QuestId, ItemId, Quantity, GrantOnAccept) VALUES (1440331284, 295715, 1, 0);
INSERT INTO questtransitions (FromQuest, ToQuest, Ordinal) VALUES (1440331284, 1440331285, 0);
INSERT INTO questwire (QuestId, Source, GiverType, GiverInstance, QuestCode, UnknownHash, Quality, TimeLimit, Unknown20, Unknown21, Unknown22, Unknown23Type, Unknown23Instance, Unknown25, Unknown26) VALUES (1440331284, 'Captured', 50000, 2052536072, 1465080153, 1112880708, 10, 0, 6, 0, 105161, 0, 0, 0, 7);
INSERT INTO questwireactions (QuestId, Ordinal, Version, ActionType, ActionInstance, Unknown1Type, Unknown1Instance, Unknown2Type, Unknown2Instance, Unknown3Type, Unknown3Instance, Unknown4Type, Unknown4Instance, Unknown5, Unknown6, Unknown7, Unknown8, Unknown9Type, Unknown9Instance, Unknown10, Unknown11, Unknown12, Unknown13, Unknown14Type, Unknown14Instance, Deadline, Unknown16, TrackingType, PlayfieldType, PlayfieldInstance, Unknown18, Unknown19, X, Y, Z) VALUES (1440331284, 0, 24, 0, 0, 0, 0, 70099, 105161, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 54001, 40016, 6553, 100000, 100000, 3413, 0, 904);
INSERT INTO questwirerewards (QuestId, Ordinal, LowId, HighId, Quality, Unknown1) VALUES (1440331284, 0, 295715, 295715, 10, 0);

-- Become a Vessel for the Spirit  [20260914-220505_s5/s5:1440331285]
-- Finishes when the spirit is worn, the same way "Install the implant" does for everyone else.
INSERT INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires, RequiresProfession) VALUES (1440331285, 'Become a Vessel for the Spirit', 'Become a Vessel for the Spirit<BR><BR>Lady Sheila Black wants you to become a Vessel for the spirit. Prove to her that you are up for this task.<BR><BR><font color="#FF0000">Mission Objective:<BR>Equip the <a href=\'itemref://295715/295715/10\'>Comfortless Spirit of Defense</a>.</font>', 2052536072, 244818, 0, 0, 6553, 0, 15);
-- Finished by Equip item 295715 QL10  [20260914-220505_s5/s5 #27342]
INSERT INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, TargetLowId, TargetHighId, TargetQuality, Required) VALUES (1440331285, 0, 6, '295715', 0, 0, 0, 1);
INSERT INTO questtransitions (FromQuest, ToQuest, Ordinal) VALUES (1440331285, 1440331288, 0);
INSERT INTO questwire (QuestId, Source, GiverType, GiverInstance, QuestCode, UnknownHash, Quality, TimeLimit, Unknown20, Unknown21, Unknown22, Unknown23Type, Unknown23Instance, Unknown25, Unknown26) VALUES (1440331285, 'Captured', 50000, 2052536072, 1363234609, 0, 0, 0, 6, 0, 105162, 0, 0, 0, 7);
INSERT INTO questwireactions (QuestId, Ordinal, Version, ActionType, ActionInstance, Unknown1Type, Unknown1Instance, Unknown2Type, Unknown2Instance, Unknown3Type, Unknown3Instance, Unknown4Type, Unknown4Instance, Unknown5, Unknown6, Unknown7, Unknown8, Unknown9Type, Unknown9Instance, Unknown10, Unknown11, Unknown12, Unknown13, Unknown14Type, Unknown14Instance, Deadline, Unknown16, TrackingType, PlayfieldType, PlayfieldInstance, Unknown18, Unknown19, X, Y, Z) VALUES (1440331285, 0, 24, 0, 0, 0, 0, 70099, 105162, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 54001, 40016, 6553, 100000, 100000, 3437, 0, 803);

-- Talk to Lady Sheila Black  [20260914-220505_s5/s5:1440331288]
-- She asks for a Blank ICC ID Chip and Biological Survey Nanobots; only the chip (296575) came back
-- as a hand-in objective in the capture, so that is what is required here.
INSERT INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires, RequiresProfession) VALUES (1440331288, 'Talk to Lady Sheila Black', 'Talk to Lady Sheila Black<BR><BR>Lady Sheila Black is now willing to to help you assemble your ID chip, and imprint your DNA in to this vital component of your future identification card. <BR><BR><font color="#FF0000">Mission Objective:<BR>Give Lady Sheila Black the following three items:<BR><a href=\'itemref://296575/296575/1\'><img src="rdb://297387"></a><BR>A Blank ICC ID Chip<BR><a href=\'itemref://296574/296574/1\'><img src="rdb://297388"></a><BR>Some Biological Survey Nanobots', 2052536072, 244818, 1400, 2596, 6553, 0, 15);
-- Finished by TradeHandIn item 296575 QL1 (linked in the stage description) Lady Sheila Black  [20260914-220505_s5/s5 #27844]
INSERT INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, TargetLowId, TargetHighId, TargetQuality, Required) VALUES (1440331288, 0, 7, '296575', 296575, 296575, 1, 1);
INSERT INTO questitemrewards (QuestId, ItemId, Quantity, GrantOnAccept) VALUES (1440331288, 296576, 1, 0);
INSERT INTO questwire (QuestId, Source, GiverType, GiverInstance, QuestCode, UnknownHash, Quality, TimeLimit, Unknown20, Unknown21, Unknown22, Unknown23Type, Unknown23Instance, Unknown25, Unknown26) VALUES (1440331288, 'Captured', 50000, 2052536072, 860764484, 1261521734, 10, 0, 6, 0, 105163, 0, 0, 0, 7);
INSERT INTO questwireactions (QuestId, Ordinal, Version, ActionType, ActionInstance, Unknown1Type, Unknown1Instance, Unknown2Type, Unknown2Instance, Unknown3Type, Unknown3Instance, Unknown4Type, Unknown4Instance, Unknown5, Unknown6, Unknown7, Unknown8, Unknown9Type, Unknown9Instance, Unknown10, Unknown11, Unknown12, Unknown13, Unknown14Type, Unknown14Instance, Deadline, Unknown16, TrackingType, PlayfieldType, PlayfieldInstance, Unknown18, Unknown19, X, Y, Z) VALUES (1440331288, 0, 24, 0, 0, 0, 0, 70099, 105163, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 54001, 40016, 6553, 100000, 100000, 3413, 0, 904);
INSERT INTO questwirerewards (QuestId, Ordinal, LowId, HighId, Quality, Unknown1) VALUES (1440331288, 0, 296576, 296576, 1, 0);

-- The fork itself: "Return to Vernon Godfray" (1439635802) already leads to Dr. Mason for everyone
-- else; a Shade goes to Sheila Black instead. Each stage is passed over for whoever it is not for.
INSERT INTO questtransitions (FromQuest, ToQuest, Ordinal) VALUES (1439635802, 1440331282, 1);
UPDATE quests SET RequiresProfession = -15 WHERE Id = 1439635805;
UPDATE quests SET RequiresProfession = -15 WHERE Id IN (1439635806, 1439635807, 1439635808, 1439635809, 1439635811, 1439635816);

-- Lady Sheila Black's conversation, as captured.
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 13, 0, 0, 'Don\'t mention it. Perhaps you should talk to Lorelei, I heard she might be involved in some... Illegal things in her spare time.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 1, 0, 0, 'Come closer my friend.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 1, 1, 1, 'I was told you could help me out...', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 1, 2, 1, 'I have a few questions...', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 1, 3, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 2, 0, 0, 'Another shade, could it be? What do you need help with, my dear?', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 2, 1, 1, 'I need my DNA imprented in this Blank ICC ID Chip..', 0, 3, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 2, 2, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 3, 0, 0, 'I could do that in a heartbeat, but there is something that is not quite right about you. I will help you with your ID chip, if you prove to me that you are a true Shade.\\nTake this Spirit Siphon and use it on a dying monster to capture its spirit. Return to me with the spirit you found.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 4, 0, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 5, 0, 0, 'There is something familiar with your face...', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 5, 1, 1, 'I found a Soul Capsule!', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 5, 2, 1, 'I lost the Nano Crystal.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 5, 3, 1, 'I have a few questions...', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 5, 4, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 6, 0, 0, 'Let me have a look.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 6, 1, 3, 'Drag and drop the item(s) you want to give to Lady Sheila Black into one of the slots available and press "accept"', 1, 7, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 7, 0, 0, 'Impressive. You can open the Soul Capsule to see what it contains. \\nI gave you a spirit as a reward, why don\'t you become its vessel?', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 7, 1, 1, 'I will.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 7, 2, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 8, 0, 0, 'Talk to me when you are done.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 8, 1, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 9, 0, 0, 'Come closer my friend.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 9, 1, 1, 'I have a few questions...', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 9, 2, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 10, 0, 0, 'I like your style.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 10, 1, 1, 'Now, will you help me?', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 10, 2, 1, 'I have a few questions...', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 10, 3, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 11, 0, 0, 'Yes. Hand me the following items:\\nA Blank ICC ID Chip and some Biological Survey Nanobots.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 11, 1, 3, 'Drag and drop the item(s) you want to give to Lady Sheila Black into one of the slots available and press "accept"', 2, 12, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 12, 0, 0, 'She touches you and it feels like she ripped out part of your spirit.. A little droplet of blood runs down your cheek. Did you just cry blood?', 1, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 12, 1, 0, 'There we have it, one personalized ID chip, ready to be used in a card.', 0, 0, 0, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 12, 2, 1, 'Eh.. thank you..', 0, 13, 1, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', 12, 3, 1, 'Goodbye', 0, 0, 2, 0);
INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES (6553, 'Lady Sheila Black', -1, 0, 0, 'I am so happy to finally meet another shade in this place.', 0, 0, 0, 0);
INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES (6553, 'Lady Sheila Black', 110, '1440331282', '', '', 1);
INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES (6553, 'Lady Sheila Black', 110, '1440331282', '', '', 2);
INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES (6553, 'Lady Sheila Black', 110, '1440331284', '', '', 5);
INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES (6553, 'Lady Sheila Black', 110, '1440331284', '', '', 6);
INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES (6553, 'Lady Sheila Black', 100, '', '', '', 9);
INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES (6553, 'Lady Sheila Black', 110, '1440331288', '', '', 10);

