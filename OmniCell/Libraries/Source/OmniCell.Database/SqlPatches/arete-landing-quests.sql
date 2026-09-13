-- Quests, extracted from live captures by Tools/Capture/AreaExtract.
--
-- Quest text/rewards and QuestAction marker fields are structured data off the
-- wire. Objective targets are resolved against captured actors, fixtures and
-- item references. Requires is OmniCell's documented reachability
-- representation, not a captured quest field.
--
-- Anything commented out below did not parse into something that could
-- be completed, and wants a person to look at it.

DELETE FROM questobjectives WHERE QuestId IN (SELECT Id FROM quests WHERE Playfield = 6553);
DELETE FROM quests WHERE Playfield = 6553;

-- 46 quests, from 114 seen; the rest were the same quest handed to another character.

-- 2 more belong to other playfields and are left out.

-- 280252049 "Kill three Leets" is given by 50000:1330923332, who is not here, so it is left out.

-- 280252050 "Find the Secondhand Peddler" is given by 50000:1330923332, who is not here, so it is left out.

-- 280581390 "Kill a Leet" is given by 50000:1330923332, who is not here, so it is left out.

-- 692357650 "Alien Invasion" is given by 50000:776476744, who is not here, so it is left out.

-- 692360531 "Return to the Alien Agency H..." is given by 50000:769272055, who is not here, so it is left out.

-- 1439586017 is associated with Flint Novak (OmniCell fallback: last talked to
-- when first seen; the available capture does not prove Flint gave this quest).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586017, 'Plant a Bug', 'Plant a Bug<BR><BR>To further incriminate Desmond Calitri, a remote audio recording device is to be placed within his office.<BR><BR><font color="#FF0000">Mission Objective:<BR>Find a suitable location in Desmond Calitri\'s office to hide the bug. Pick up (Left Click) the <a href=\'itemref://295801/295801/1\'>RC-P Audio Recording Device</a> in your inventory and drop it (Left Click) it in a suitable location.</font>', 2052536068, 11342, 0, 0, 6553, 1439635571);
-- 1439586017: fixture 1477021842, template 295738, standing 0.3 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586017, 0, 4, '1477021842', 1);


-- 1439586025 is given by Flint Novak (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586025, 'Deliver the Rebuilt HC-12 Se...', 'Deliver the Rebuilt HC-12 SecTec Monitor<BR><BR>With the Surveillance Droid feed uplink and a hidden audio recording device in Desmond Calitri\'s office, it is time to deliver this potential evidence to one of Alex\'s friend ICC Immigration Officer Bill.<BR><BR><font color="#FF0000">Mission Objective:<BR>Give the <a href=\'itemref://295800/295800/1\'>Rebuilt HC-12 SecTec Monitor</a> to ICC Immigration Officer Bill.</font>', 2052536068, 158429, 1160, 2229, 6553, 1439586017);
-- 1439586025: ICC Immigration Officer Bill, standing 0.7 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439586025, 0, 7, 'Rebuilt HC-12 SecTec Monitor', 1, 295800, 295800, 1);


-- 1439586026 is given by ICC Immigration Officer Bill (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586026, 'Kneecapping a Kneebreaker', 'Kneecapping a Kneebreaker<BR><BR>While monitoring the audio and video feeds of Desmond Calitri, it became clear that he intends to send "The Kneebreaker", Alfonzo Rizzolo, to deal with an upstart Dockworker who is fighting for fair working conditions.<BR><BR><font color="#FF0000">Mission Objective:<BR>Kill "The Kneebreaker".</font>', 2052536070, 11330, 0, 0, 6553, 1439586025);
-- The mission text explicitly requires the target to be killed.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586026, 0, 0, 'Kneebreaker Alfonzo Rizzolo', 1);


-- 1439586033 "Report to Alex" is given by 0:5715715, who is not here, so it is left out.

-- 1439586034 is given by Alex Gibbs (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586034, 'Talk to Stan Goodman', 'Talk to Stan Goodman<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>Alex told you to go talk to Stan Goodman, a local \'purveyer of recently used merchandise\'. He should be able to help with aquiring more parts for your ID card.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Stan Goodman.</font>', 2052536065, 244818, 0, 0, 6553, 1439586026);
-- 1439586034: Stanley Goodman, standing 1.0 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586034, 0, 3, 'Stanley Goodman', 1);


-- 1439586035 is given by Alex Gibbs (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586035, 'Tradeskilling (1/4): Assembl...', 'Tradeskilling (1/4): Assemble a Nano Sensor<BR><BR><font color="#FF0000">WARNING: If you are interested in learning tradeskilling, this mission will help you learn the basics. However, only Engineers and Traders are equipped with profession tools to help them master the art of tradeskilling.</font><BR><BR>Alex Gibbs has provided you with the recipe for creating a <a href=\'itemref:// 156026/156027/1\'>Personalized Basic Robot Brain</a>. Once this mission has been completed, allow her to inspect it. <BR><BR><font color="#FFFFFF">1. Buy the following item from the <a href=\'itemref://297281/297281/1\'>Junk Shop</a>:<BR><a href=\'itemref://150922/150922/1\'><img src="rdb://151011"> Screwdriver</a><BR><BR>2. Find <a href=\'itemref://42620/42620/1\'>Robot Junk</a>.<BR>Do so by killing and looting a robot.<BR><BR>3. Modify the <a href=\'itemref://42620/42620/1\'>Robot Junk</a> with the <a href=\'itemref://150922/150922/1\'>Screwdriver</a> to create a <a href=\'itemref://150923/150924/1\'>Nano Sensor</a>.<BR><a href=\'itemref://150922/150922/1\'><img src="rdb://151011"></a> + <a href=\'itemref://42620/42620/1\'><img src="rdb://290417"></a> = <a href=\'itemref://150923/150923/1\'><img src="rdb://149940"></a><BR></font><BR><font color="#FF0000">Mission Objective: Open the Tradeskill Kit %{KEY:WINDOW_TS}%, place the <a href=\'itemref://150922/150922/1\'>Screwdriver</a> as the Source and the <a href=\'itemref://42620/42620/1\'>Robot Junk</a> as the Target, then press Build.</font>', 2052536065, 11340, 0, 0, 6553, 1439586034);
-- 1439586035: build Nano Sensor.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586035, 0, 9, 'Nano Sensor', 1);


-- 1439586038 is given by Stanley Goodman (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586038, 'Buy a Lockpick', 'Buy a Lockpick<BR><BR>Stan told you to Pick the Lock on the Strongbox in the Merchant\'s Storage undetected, but in order to do so you need to buy a <a href=\'itemref://95577/95577/1\'>Lock Pick</a>.<BR><BR><font color="#FF0000">Mission Objective:<BR>Find the <a href=\'itemref://297290/297290/3\'>ICC Tech Supplies</a> vending machine and buy a <a href=\'itemref://95577/95577/1\'>Lock Pick</a>.</font>', 2052536069, 244818, 0, 0, 6553, 1439586035);
-- Buying the Lock Pick is the objective. Merely opening its vending machine
-- must not complete the mission.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586038, 0, 8, 'Lock Pick', 1);


-- 1439586041 is given by Sarah Greene (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586041, 'Take the contents of the Str...', 'Take the contents of the Strongbox <BR><BR>Stan told you to Pick the Lock on the Strongbox in the Merchant\'s Storage undetected. Now that you have bought a <a href=\'itemref://95577/95577/1\'>Lock Pick</a>, this should be an easy task.<BR><BR><font color="#FF0000">Mission Objective:<BR>Pick up (Left Click) your <a href=\'itemref://95577/95577/1\'>Lock Pick</a> from your inventory and drop it (Left Click) on the <a href=\'itemref://295604/295604/1\'>Merchant\'s Strongbox</a>.</font>', 2052536073, 244818, 0, 0, 6553, 1439586038);
-- Use the Lock Pick on the named strongbox; reaching its coordinates is not completion.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586041, 0, 2, '900000001', 1);


-- 1439586046 is given by Sarah Greene (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586046, 'Deliver Antonio\'s Adaptation...', 'Deliver Antonio\'s Adaptation Factory to Stan Goodman.<BR><BR>Stan told you to Pick the Lock on the Strongbox in the Merchant\'s Storage undetected. Now that you have found <a href=\'itemref://248306/248306/1\'>Antonio\'s Adaptation Factory</a>, bring it back to Stan.<BR><BR><font color="#FF0000">Mission Objective:<BR>Bring <a href=\'itemref://248306/248306/1\'>Antonio\'s Adaptation Factory</a> to Stan Goodman.</font>', 2052536073, 158429, 1240, 2581, 6553, 1439586041);
-- 1439586046: Stanley Goodman, standing 1.0 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439586046, 0, 7, 'Antonio''s Adaptation Factory', 1, 248306, 248306, 1);


-- 1439586048 is given by Stanley Goodman (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586048, 'Talk to Sarah Greene', 'Talk to Sarah Greene<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>Stab told you that Sarah Greene, a local armorsmith, should be able to help you with aquiring more parts needed for your ID card.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Sarah Greene.</font>', 2052536069, 244818, 0, 0, 6553, 1439586046);
-- 1439586048: Sarah Greene, by its full name - its marker at 3467,841 has nobody on it.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586048, 0, 3, 'Sarah Greene', 1);


-- 1439586049 is given by Stanley Goodman (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586049, 'Buy some Nano Programs', 'Buy some Nano Programs<BR><BR>Stanley Goodman told you to go talk to Marco Spida to buy a Nanoprogram Container.<BR><BR><font color="#FF0000">Mission Objective: Talk to Marco Spida and buy a Nanoprogram Container for your profession. Open the Container to complete your mission.</font>', 2052536069, 244818, 1160, 2581, 6553, 1439586048);
-- 1439586049: Marco Spida, standing 0.4 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586049, 0, 3, 'Marco Spida', 1);


-- 1439586052 is given by Sarah Greene (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586052, 'Find the thief', 'Find the thief<BR><BR>Sarah recently had one of her custom-built suits of armor stolen from her. The thief was last seen in the underground.<BR><BR><font color="#FF0000">Mission Objective:<BR>Locate the thief and recover the DNA-Locked Armor.</font>', 2052536073, 244818, 0, 0, 6553, 1439586049);
-- The located thief must be killed; talking to it cannot satisfy the mission.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586052, 0, 0, 'Mutated Garbage Flea', 1);


-- 1439586063 is given by Sarah Greene (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586063, 'Deliver DNA-Locked Armor to ...', 'Deliver DNA-Locked Armor to Sarah Greene<BR><BR>You have found the stolen suit of armor. <BR><BR><a href=\'itemref://295618/295618/1\'><img src="rdb://88053"></a><BR><font color="#FFFFFF">Return the DNA-Locked Armor to Sarah Greene.</font>', 2052536073, 158429, 1280, 2581, 6553, 1439586052);
-- 1439586063: Sarah Greene, by its full name - its marker at 3467,841 has nobody on it.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439586063, 0, 7, 'DNA-Locked Prototype Raven Combat Armor', 1, 295618, 295618, 200);


-- 1439586064 is given by Sarah Greene (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586064, 'Speak to Vernon Godfray', 'Speak to Vernon Godfray<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>Sarah told you to speak to Vernon Godfray, a local hacker, who should be able to help with aquiring more parts needed for your ID card.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Vernon Godfray.</font>', 2052536073, 244818, 0, 0, 6553, 1439586063);
-- 1439586064: Vernon Godfray, standing 0.9 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586064, 0, 3, 'Vernon Godfray', 1);


-- 1439586067 is given by Vernon Godfray (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586067, 'Hacking Skills', 'Hacking Skills<BR><BR>Vernon Godfray told you to hack the OT Technical Library to allow it to be worn by anyone.<BR><BR>Use the <a href=\'itemref://87810/87810/1\'>Hacker Tool</a> to hack the <a href=\'itemref://248377/248377/1\'> Omni-Tek Technical Library</a> to create the <a href=\'itemref://295756/295756/1\'>Hacked Technical Library</a>.<BR><a href=\'itemref://87810/87810/1\'><img src="rdb://99282"></a> + <a href=\'itemref://248377/248377/1\'><img src="rdb://130564"></a> = <a href=\'itemref://295756/295756/1\'><img src="rdb://130561"></a><BR><BR><font color="#FF0000">Mission Objective: Open the Tradeskill Kit %{KEY:WINDOW_TS}%, place the <a href=\'itemref://87810/87810/1\'>Hacker Tool</a> as the Source and the <a href=\'itemref://248377/248377/1\'> Omni-Tek Technical Library</a> as the Target, then press Build.</font>', 2052536072, 11340, 0, 0, 6553, 1439586064);
-- The tradeskill result is what completes this stage.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586067, 0, 9, 'Hacked Technical Library', 1);


-- 1439586079 is given by Vernon Godfray (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586079, 'Give the Hacked Technical Li...', 'Give the Hacked Technical Library to Vernon Godfray<BR><BR>You successfully hacked the OT Technical Library. You should return it to Vernon Godfray.<BR><BR><font color="#FF0000">Mission Objective:<BR>Give the <a href=\'itemref://295756/295756/1\'>Hacked Technical Library</a> to Vernon Godfray.</font>', 2052536072, 158429, 1320, 2581, 6553, 1439586067);
-- 1439586079: Vernon Godfray, standing 0.9 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439586079, 0, 7, 'Hacked Technical Library', 1, 295756, 295756, 1);


-- 1439586080 is given by Vernon Godfray (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586080, 'Cargo Lifting', 'Cargo Lifting<BR><BR>Vernon mentioned that he would like to get his hands on the data from one of the Shipping Manifest Terminals located in the industrial district of the shuttleport.<BR><BR><font color="#FF0000">Mission Objective:<BR>Open a dialog with the Shipping Manifest Terminal and apply the <a href=\'itemref://87810/87810/1\'>Hacker Tool</a> if access is denied.</font>', 2052536072, 244818, 0, 0, 6553, 1439586079);
-- Apply the Hacker Tool to the terminal.
-- The terminal is a character-shaped world object, so UseItemOn receives its
-- stable instance rather than an item-template name.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586080, 0, 2, '2052536074', 1);


-- 1439586083 is given by Shipping Manifest Terminal (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586083, 'Return to Vernon Godfray', 'Return to Vernon Godfray<BR><BR>After finishing the hack job, return to Vernon and he might help you with your ID problem.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Vernon Godfray and give him the <a href=\'itemref://296572/296572/1\'>Unprogrammed Identification Chip</a>.</font>', 2052536074, 158429, 1360, 2596, 6553, 1439586080);
-- 1439586083: Vernon Godfray, standing 0.9 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439586083, 0, 7, 'Unprogrammed Identification Chip', 1, 296572, 296572, 1);


-- 1439586087 is given by Vernon Godfray (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586087, 'Talk to Doctor Mason', 'Talk to Doctor Mason<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>After helping Vernon, he gave you a Blank ICC ID Chip. He said that Dr Mason would be able to help you out further to imprint your DNA in to the chip.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Doctor Mason.</font>', 2052536072, 244818, 0, 0, 6553, 1439586083);
-- 1439586087: Dr. Mason, standing 0.4 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586087, 0, 3, 'Dr. Mason', 1);


-- 1439586090 is given by Dr. Mason (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586090, 'Assemble an Implant (1/3)', 'Assemble an Implant (1/3)<BR><BR>Doctor Mason has provided you with the recipe for creating a <a href=\'itemref:// 113440/113441/1\'>Leg Implant: Agility, Shiny</a>. Once it has been completed, allow him to inspect it. <BR><BR><font color="#FFFFFF">1. Buy a <a href=\'itemref:// 101261/101262/1\'>A Basic Leg Implant</a> from the <a href=\'itemref:// 297320/ 297320/1\'>ICC Basic Implants</a> vending machine.<BR><BR>2. Buy a <a href=\'itemref://101781/101782/50\'>Agility Cluster - Shiny (Leg)</a> from the <a href=\'itemref:// 297323/ 297323/1\'>ICC Shiny Clusters</a> vending machine.<BR><BR>3. Insert the <a href=\'itemref://101781/101782/50\'>Agility Cluster - Shiny (Leg)</a> in to the <a href=\'itemref:// 101261/101262/5\'>A Basic Leg Implant</a> to create a <a href=\'itemref:// 113127/113128/5\'>Leg Implant: Agility, Shiny</a>.<BR><a href=\'itemref://101781/101782/50\'><img src="rdb://35985"></a> + <a href=\'itemref:// 101261/101262/5\'><img src="rdb://12688"></a> = <a href=\'itemref:// 113127/113128/5\'><img src="rdb://12689"></a><BR></font><BR><font color="#FF0000">Mission Objective: Open the Tradeskill Kit %{KEY:WINDOW_TS}%, place the <a href=\'itemref://101781/101782/50\'>Agility Cluster - Shiny (Leg)</a> as the Source and the <a href=\'itemref:// 101261/101262/5\'>A Basic Leg Implant</a> as the Target, then press Build.</font>', 2052536076, 11340, 0, 0, 6553, 1439586087);
-- 1439586090: build Leg Implant: Agility, Shiny.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586090, 0, 9, 'Leg Implant: Agility, Shiny', 1);


-- 1439586098 is given by Dr. Mason (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586098, 'Assemble an Implant (2/3)', 'Assemble an Implant (2/3)<BR><BR>Doctor Mason has provided you with the recipe for creating a <a href=\'itemref:// 113440/113441/1\'> Leg Implant: Agility, Shiny</a>. Once it has been completed, allow him to inspect it. <BR><BR><font color="#FFFFFF">1. Buy a <a href=\'itemref://101785/101786/1\'>Stamina Cluster - Bright (Leg)</a> from the <a href=\'itemref://297322/297322/1\'>ICC Bright Clusters</a> vending machine. <BR><BR>2. Insert the <a href=\'itemref://101785/101786/1\'>Stamina Cluster - Bright (Leg)</a> in to the <a href=\'itemref:// 113127/113128/5\'>Leg Implant: Agility, Shiny</a> to create a <a href=\'itemref://113186/113187/5\'>Leg Implant: Agility, Shiny</a>.<BR><a href=\'itemref://101785/101786/50\'><img src="rdb://35984"></a> + <a href=\'itemref://113127/113128/5\'><img src="rdb://12689"></a>= <a href=\'itemref://113186/113187/5\'><img src="rdb://12689"></a><BR></font><BR><font color="#FF0000">Mission Objective: Open the Tradeskill Kit %{KEY:WINDOW_TS}%, place the <a href=\'itemref://101785/101786/1\'>Stamina Cluster - Bright (Leg)</a> as the Source and the <a href=\'itemref:// 113127/113128/5\'>Leg Implant: Agility, Shiny</a> as the Target, then press Build.</font>', 2052536076, 11340, 0, 0, 6553, 1439586090);
-- 1439586098: build Leg Implant: Agility, Shiny.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586098, 0, 9, 'Leg Implant: Agility, Shiny', 1);


-- 1439586102 is given by Dr. Mason (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586102, 'Assemble an Implant (3/3)', 'Assemble an Implant (3/3)<BR><BR>Doctor Mason has provided you with the recipe for creating a <a href=\'itemref:// 113440/113441/1\'> Leg Implant: Agility, Shiny</a>. Once it has been completed, allow him to inspect it. <BR><BR><font color="#FFFFFF">1. Buy a <a href=\'itemref://101807/101808/1\'>Max Health Cluster - Faded (Leg)</a> from the <a href=\'itemref://297321/297321/1\'>ICC Faded Clusters</a> vending machine.<BR><BR>2. Insert the <a href=\'itemref://101807/101808/1\'>Max Health Cluster - Faded (Leg)</a> in to the <a href=\'itemref://113186/113187/5\'>Leg Implant: Agility, Shiny</a> to create a <a href=\'itemref://113440/113441/5\'>Leg Implant: Agility, Shiny</a>.<BR><a href=\'itemref://101785/101786/50\'><img src="rdb://35983"></a> + <a href=\'itemref://113127/113128/5\'><img src="rdb://12689"></a> = <a href=\'itemref:// 113440/113441/5\'><img src="rdb://12689"></a><BR></font><BR><font color="#FF0000">Mission Objective: Open the Tradeskill Kit %{KEY:WINDOW_TS}%, place the <a href=\'itemref://101807/101808/1\'>Max Health Cluster - Faded (Leg)</a> as the Source and the <a href=\'itemref://113186/113187/5\'>Leg Implant: Agility, Shiny</a> as the Target, then press Build.</font>', 2052536076, 11340, 0, 0, 6553, 1439586098);
-- 1439586102: build Leg Implant: Agility, Shiny.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586102, 0, 9, 'Leg Implant: Agility, Shiny', 1);


-- 1439586106 is given by Dr. Mason (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586106, 'Show Dr. Mason your brand ne...', 'Show Dr. Mason your brand new Implant<BR><BR>Doctor Mason has provided you with the recipe for creating a <a href=\'itemref:// 113440/113441/1\'>Leg Implant: Agility, Shiny</a>. Once it has been completed, allow him to inspect it. <BR><BR><font color="#FF0000">Mission Objective:<BR>Return the implant to Dr. Mason for inspection.</font>', 2052536076, 244818, 0, 0, 6553, 1439586102);
-- 1439586106: Dr. Mason, standing 0.4 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586106, 0, 3, 'Dr. Mason', 1);


-- 1439586109 is given by Dr. Mason (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586109, 'Install the implant', 'Install the implant<BR><BR>Dr. Mason has given you a complimentary leg implant as a gift. As a final test, Dr. Mason wants you to install it.<BR><BR><font color="#FF0000">Mission Objective:<BR>Equip the <a href=\'itemref:// 295706/295706/1\'>Leg Implant: Agility, Shiny</a> after activating the Stationary Automated Surgery Clinic.</font>', 2052536076, 244818, 0, 0, 6553, 1439586106);
-- 1439586109: wear Leg Implant: Agility, Shiny.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586109, 0, 6, 'Leg Implant: Agility, Shiny', 1);


-- 1439586112 is given by Dr. Mason (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586112, 'Talk to Doctor Mason', 'Talk to Doctor Mason<BR><BR>Doctor Mason at the hospital is willing to help you assemble your ID chip, and imprint your DNA in to this vital component of your future identification card. <BR><BR><font color="#FF0000">Mission Objective:<BR>Give Dr Mason the following two items:<BR><BR><a href=\'itemref://296575/296575/1\'><img src="rdb://297387"></a><BR>A Blank ICC ID Chip<BR><a href=\'itemref://296574/296574/1\'><img src="rdb://297388"></a><BR>Some Biological Survey Nanobots', 2052536076, 244818, 1400, 2596, 6553, 1439586109);
-- 1439586112: Dr. Mason, standing 0.4 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439586112, 0, 7, 'Blank ICC ID Chip', 1, 296575, 296575, 1);
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439586112, 1, 7, 'Biological Survey Nanobots', 1, 296574, 296574, 1);


-- 1439586114 is given by Dr. Mason (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586114, 'Talk to Lorelei', 'Talk to Lorelei<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>You have been pointed towards a young woman named Lorelei, a bartender at one of the local watering holes. She apparently has quite the successful \'side business\', and should be able to help with creating your needed ID card.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Lorelei the Bartender.</font>', 2052536076, 244818, 0, 0, 6553, 1439586112);
-- 1439586114: Lorelei the Bartender, standing 1.2 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586114, 0, 3, 'Lorelei the Bartender', 1);


-- 1439586115 is given by Lorelei the Bartender (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439586115, 'Lorelei\'s Lost Pet', 'Lorelei\'s Lost Pet<BR><BR>The bartender Lorelei has lost her reet pet. The bird named Lolly escaped from its cage. She thinks the reet has found a friend, because it normally comes home.<BR>Locate the escape artist and get it back in to its cage.<BR><BR><font color="#FF0000">Mission Objective:<BR>Capture Lorelei\'s Reet Pet.</font>', 2052536075, 244818, 0, 0, 6553, 1439586114);
-- The task is to locate Lolly, not to speak to the quest giver again.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439586115, 0, 3, 'Lolly the Reet', 1);


-- 1439635504 is given by Lolly the Reet (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635504, 'Deliver the Reet to Lorelei ', 'Deliver the Reet to Lorelei <BR><BR>After finally catching the silly bird, return to Lorelei to hand it back.<BR><BR><font color="#FF0000">Mission Objective:<BR>Give Lorelei the <a href=\'itemref://297367/297367/1\'>Pet Cage With a Reet</a>.</font>', 2054140095, 158429, 1440, 2596, 6553, 1439586115);
-- 1439635504: Lorelei the Bartender, standing 1.2 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439635504, 0, 7, 'Pet Cage With a Reet', 1, 297367, 297367, 1);


-- 1439635506 is given by Lorelei the Bartender (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635506, 'Talk to Vaughn Hammond', 'Talk to Vaughn Hammond<BR><BR>Your ID card is finally complete! Talk to Vaughn Hammond about leaving Arete Landing.', 2052536075, 244818, 1040, 2596, 6553, 1439635504);
-- 1439635506: Vaughn Hammond, by its full name - its marker at 0,0 has nobody on it.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635506, 0, 3, 'Vaughn Hammond', 1);


-- 1439635548 is associated with Rex Larsson. Its captured QuestGiver chain
-- identity and dialogue both name Rex, but no conversation-open handover was
-- captured for this per-character quest instance.
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635548, 'Terminate 5 Malfunctioning C...', 'Terminate 5 Malfunctioning Cleaning Robots<BR><br><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<br><BR>Rex Larsson considers himself too lazy to clean up his cleaning business. Since you need his help, he wanted a favor in return. You have to terminate 5 of his Malfunctioning Cleaning Robots then open the package with brand new cleaning robots and set them to work.<BR><BR><font color="#FF0000">Mission Objective:<BR>Kill 5 Malfunctining Cleaning Robots.</font>', 2052536067, 11330, 0, 0, 6553, 0);
-- 1439635548: Malfunctioning Cleaning Robot, by its full name - its marker at 3614,779 has nobody on it.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635548, 0, 0, 'Malfunctioning Cleaning Robot', 5);


-- 1439635551 is given by Rex Larsson (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635551, 'Open the Cargo Box', 'Open the Cargo Box<BR><BR>Rex Larsson considers himself too lazy to clean up his cleaning business. Since you need his help, he wanted a favor in return. You have to terminate 5 of his Malfunctioning Cleaning Robots then open the Cargo Box with brand new cleaning robots and set them to work.<BR><BR><font color="#FF0000">Mission Objective:<BR>Use (Right Click) the Cargo Box to open it.</font>', 2052536067, 244818, 0, 0, 6553, 1439635548);
-- 1439635551: fixture 1477021841, template 297277, standing 1.6 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635551, 0, 4, '1477021841', 1);


-- 1439635552 is given by Rex Larsson (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635552, 'Return to Rex Larsson', 'Return to Rex Larsson<BR><BR>Return to Rex Larsson to inform him of the great cleaning success.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Rex Larsson.</font>', 2052536067, 244818, 1040, 1281, 6553, 1439635551);
-- 1439635552: Rex Larsson, by its full name - its marker at 3621,790 has nobody on it.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635552, 0, 3, 'Rex Larsson', 1);


-- 1439635555 is given by Rex Larsson (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635555, 'Talk to Marcus Stone', 'Talk to Marcus Stone<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>Rex Larsson told you to spreak with Marcus Stone, an overseer for arriving cargo in the area, might be able to aid in getting your license issue settled.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Marcus Stone.</font>', 2052536067, 244818, 0, 0, 6553, 1439635552);
-- 1439635555: Marcus Stone, by its full name - its marker at 3638,830 has nobody on it.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635555, 0, 3, 'Marcus Stone', 1);


-- 1439635556 is given by Marcus Stone (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635556, 'Extinguish the Gas Fire', 'Extinguish the Gas Fire<BR><BR>Marcus Stone mentioned that he may be able to assist you with your lack of identity on Rubi-Ka, but at a price. A recent accident on one of his landing pads has left cargo damaged and people injured. Bodies can heal while cargo cannot. Extinguish one of the Gas Fires that has errupted on the landing pad.<BR><BR><font color="#FF0000">Mission Objective:<BR>(Left Click) the <a href=\'itemref://296780/296780/1\'>Compact Fire Suppressant Container</a> in your inventory to lift it up, then Left Click the Gas Fire to apply the fire suppressant.</font>', 2052536066, 244818, 0, 0, 6553, 1439635555);
-- Apply the fire suppressant to the fire.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635556, 0, 2, 'Gas Fire', 1);


-- 1439635562 is given by Marcus Stone (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635562, 'Return to Marcus', 'Return to Marcus<BR><BR>After extinguishing one of the Gas Fires on Marcus\' landing platform, he may be more willing to help you with your lack of identity.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Marcus Stone and hand him the <a href=\'itemref://296780/296780/1\'>Compact Fire Suppressant Container</a>.</font>', 2052536066, 158429, 1080, 1281, 6553, 1439635556);
-- 1439635562: Marcus Stone, by its full name - its marker at 0,0 has nobody on it.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439635562, 0, 7, 'Compact Fire Suppressant Container', 1, 296780, 296780, 1);


-- 1439635563 is given by Marcus Stone (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635563, 'Talk to Flint Novak', 'Talk to Flint Novak<BR><BR><font color="#63ad63">Identity Crisis:</font><BR>In order to leave Arete Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a fake ID Card to you can leave this place..<BR><BR>Marcus Stone told you to speak with Flint Novak, who oversees the local junkyard. He should be able to help with aquiring the parts for an ID card.<BR><BR><font color="#FF0000">Mission Objective:<BR>Talk to Flint Novak.</font>', 2052536066, 244818, 0, 0, 6553, 1439635562);
-- 1439635563: Flint Novak, standing 0.6 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635563, 0, 3, 'Flint Novak', 1);


-- 1439635564 is given by Flint Novak (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635564, 'Find a Bio Analyzing Computer', 'Find a Bio Analyzing Computer<BR><BR>At the request of Flint Novak you must  find a Bio Analyzing Computer. You may find one of these computers by taking out the malfunctioning robots in the nearby junkyard.<BR><BR><font color="#FF0000">Mission Objective:<BR>Kill 7 Robots in the junkyard.</font>', 2052536068, 11330, 0, 0, 6553, 1439635563);
-- The mission text requires seven robot kills, not arrival at the marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635564, 0, 0, 'Malfunctioning Cleaning Robot', 7);


-- 1439635570 is given by Flint Novak (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635570, 'Deliver the Bio Analyzing Co...', 'Deliver the Bio Analyzing Computer to Alex Gibbs<BR><BR>After killing a few junk robots you finally found a Bio Analyzing Computer. Flint Novak told you to give this to Alex Gibbs, a local roboticist.<BR><BR><font color="#FF0000">Mission Objective:<BR>Give the <a href=\'itemref://156020/156021/1\'>Bio Analyzing Computer</a> to Alex Gibbs.</font>', 2052536068, 158429, 1120, 2076, 6553, 1439635564);
-- 1439635570: Alex Gibbs, standing 1.0 units from its marker.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required, TargetLowId, TargetHighId, TargetQuality) VALUES (1439635570, 0, 7, 'Bio Analyzing Computer', 1, 156020, 156021, 1);


-- 1439635571 is given by Alex Gibbs (the log gained it while talking to them).
REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES (1439635571, 'Surveillance Uplink', 'Surveillance Uplink<BR><BR>Alex Gibbs has provided you with a contraption that will be able to hook into the video feed one of Desmond Calitri\'s Surveillance Droids.<BR><BR><font color="#FF0000">Mission Objective:<BR>Target the Surveillance Droid and use (Right Click) the <a href=\'itemref://295800/295800/1\'>Rebuilt HC-12 SecTec Monitor in your inventory.</a></font>', 2052536065, 244818, 0, 0, 6553, 1439635570);
-- Use the rebuilt monitor on the surveillance droid.
-- The droid is a character-shaped world object, so UseItemOn receives its
-- stable instance rather than an item-template name.
REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required) VALUES (1439635571, 0, 2, '2052536106', 1);
