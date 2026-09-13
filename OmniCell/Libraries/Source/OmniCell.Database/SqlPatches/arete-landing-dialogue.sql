-- The initial conversation rows below are what characters of playfield 6553
-- said in the decoded captures. Later UPDATE/INSERT sections are separately
-- labelled emulator-owned controls; do not attribute those controls or their
-- fallback wording to the retail server.
--
-- One walk per character - the longest one any capture heard. A
-- conversation is a tree and a session walks one path through it, so an
-- option nobody clicked is in no capture and cannot be here.

DELETE FROM knubotscript WHERE Playfield = 6553;

-- Alex Gibbs
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 0, 0, 0, 'Hello there, what do you want?', 1439635571);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 0, 1, 1, 'I was told to bring you this device so you could help me get an ID.', 1439635571);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 0, 2, 1, 'Goodbye', 1439635571);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 1, 3, 0, 'Wait.. You came onto Rubi-Ka without sufficient identification? What? I don\'t even... I need a moment to think... I am only experienced with providing identification to newly created automations. While the process is similar in nature, I can only provide you with the most basic identification board.\\n\\nTo imprint that with an actual identification that would be recognized by ICC, you would need to go elsewhere. I can help you with part of the process though... if you\'re willing to potentially give me a hand.. \\n\\nLet me see what you have for me...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 1, 4, 0, 'It\'s only part of a broken robot and I already have this one.. Ah well, you have to do me a favour if I\'m going to help you further with your ID problem. Have you heard of Desmond Calitri? He is the administrator who operates and oversees all the operations around this industrial area of Arete Landing and has been starting to cut budgets for my engineering work. I want you to find out why, simply remotely hack into one of his patrolling security droids, and then plant an audio recording device in his office.\\n\\nOnce you\'re done, talk to old Bill. He\'s the closest contact I have with the ICC officials. He is usually down at the Bronto Dog Kiosk this time of the day.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 1, 5, 1, 'I will get it done.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 1, 6, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 2, 7, 0, 'Thank you, {name}.', 1439586025);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 2, 8, 1, 'Goodbye', 1439586025);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 3, 9, 0, 'Whats up?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 3, 10, 1, 'I have some questions.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 3, 11, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 4, 12, 0, 'Sure, what do you want to know?', 1439586033);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 4, 13, 1, 'Who are you?', 1439586033);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 4, 14, 1, 'What do you do around here?', 1439586033);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 4, 15, 1, 'Have you been working here for a while?', 1439586033);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 4, 16, 1, 'What goes on here?', 1439586033);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 4, 17, 1, 'Who is in charge around here?', 1439586033);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 4, 18, 1, 'Goodbye', 1439586033);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 5, 19, 0, 'Be careful out there. Remember: robots don\'t kill, but bad programming does!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 5, 20, 0, 'Whats up?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 5, 21, 1, 'I don\'t think you will need to worry about Calitri any longer.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 5, 22, 1, 'I have some questions.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 5, 23, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 6, 24, 0, 'Smiling brightly Alex squeals a bit, perhaps a little bit <i>too</i> happy to hear the tales of your sabotage and murder.', 1439586035);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 6, 25, 0, 'Fantastic! Here, take this encryption compiler - Without (A) something to encrypt, and (B) some encryption <i>codes</i> it\'s fairly worthless... but if you can get your hands on a proper ID chip <i>and</i> some authentic ICC security codes... Talk to Stan Goodman, a local merchant, he might be able to help you out with the next step in the process.\\n\\nOh, if you need any help crafting items, I\'m here to help you!', 1439586035);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 6, 26, 1, 'Can you tell me how to make a Personalized Robot Brain?', 1439586035);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 6, 27, 1, 'Goodbye', 1439586035);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 7, 28, 0, 'It is quite simple, really. All you need to find is some \'Robot Junk\' the rest can be bought from my Junk Shop. You can loot \'Robot Junk\' from robot remains. Just kill a robot and you might have a chance of salvaging its parts. The higher quality \'Robot Junk\', the better \'Personalized Robot Brain\' you will make. I will upload the recipe for making a \'Personalized Robot Brain\' to your ncu.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536065, 6553, 7, 29, 1, 'Goodbye', 0);

-- Marcus Stone
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 0, 30, 0, 'As you might have noticed, I\'m a little busy at the moment.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 0, 31, 1, 'Actually, Rex said you could help me... I seem to have misplaced my Identity Card.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 0, 32, 1, 'Who are you?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 0, 33, 1, 'Where am I?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 0, 34, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 1, 35, 0, 'Well now. Sounds like you got a problem. I\'m not going to say I <i>can\'t</i> help you but, well, as you\'ll find out soon enough, everyone on Rubi-Ka needs <i>something</i>..', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 1, 36, 1, 'So, let me guess... You need some help with the fire?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 1, 37, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 2, 38, 0, 'A little smirk crosses his lips as he nods his approval.', 1439635562);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 2, 39, 0, 'Yeah, now you\'re catching on. As you have already noticed, a few, small fires have erupted up on this landing platform. The appropriate authorities are on their way to deal with the wounded and property damage, but if you wouldn\'t mind helping me with a few of these fires, it would be appreciated. You do that, I\'ll see if I can\'t help you out with your little... identity issue.', 1439635562);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 2, 40, 1, 'Goodbye', 1439635562);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 3, 41, 0, 'Yep, off you go.', 1439635563);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 3, 42, 0, 'You did a pretty good job there!', 1439635563);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 3, 43, 1, 'Thanks.', 1439635563);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 3, 44, 1, 'Goodbye', 1439635563);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 4, 45, 0, 'Hand me the Compact Fire Suppressant Container, please.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 4, 46, 0, 'And, good to my word.... ', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 4, 47, 0, 'Reaching into a small pouch by his side he produces a thin, flat, metallic disc, barely larger than a fingernail.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 4, 48, 0, 'There. That\'s a generic nano transmitter, pretty standard equipment for... well, most everything. Same gizmo that lets you see things like data and information via your NCU\'s heads-up-display. Most of \'em are already programmed with something or another - Manufacturer\'s information, warning notes, care & use & feeding or whatever of whatever it is you\'re looking at. Typically blank ones aren\'t issued to the public - Supposed to make \'em hard to fake... Now, you go talk to Flint Novak. He runs the junkyard. Just head on down for the bottom of the ramp and look for a little shack. Can\'t miss it.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 4, 49, 1, 'Thanks again.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 4, 50, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 5, 51, 0, 'Was there anything else?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 5, 52, 1, 'Who are you?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 5, 53, 1, 'Where am I?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 5, 54, 1, 'Are those wounded workers your guys?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536066, 6553, 5, 55, 1, 'Goodbye', 0);

-- Rex Larsson
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 0, 56, 0, 'Hi, I\'m Rex. \\nNice moves, I\'m impressed. It is not easy hiding in a cargo ship. Say, how did you do it?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 0, 57, 1, 'I don\'t really feel like telling you any of my secrets. If you\'ll excuse me, I need to go now.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 0, 58, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 1, 59, 0, 'You have it all figured out, don\'t you? I bet you forgot that you need an ID card to enter ICC. I don\'t even need glasses to notice that you don\'t have anything of value...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 1, 60, 0, 'Rex lowers his voice.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 1, 61, 0, 'You know, I arrived here the same way as you just did. I might know a guy that can help you out... I got a fake ID myself.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 1, 62, 1, 'Who said I don\'t have any ID?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 1, 63, 1, 'Tell me who to talk to.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 1, 64, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 2, 65, 0, 'Alright there, boss, say what you want. Just know that if you don\'t have any form of identification, your options are limited. You can either take a quick stroll \\n\\nOr.. you could get my help getting an ID, if you start helping me out here instead of denying facts...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 2, 66, 1, 'Get to the point, Rex.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 2, 67, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 3, 68, 0, 'Ya see these Cleaning Robots? They are old, stupid and broken now. I need you to terminate them and open the cargo box with the new ones so they can start cleaning up this place... It looks messy here, don\'t you agree?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 3, 69, 1, 'I\'ll do it if you promise to tell me who your contact is.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 3, 70, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 4, 71, 0, 'Excellent choice, I will tell you what you need to know when you have completed the task.', 1439635551);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 4, 72, 1, 'Goodbye', 1439635551);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 5, 73, 0, 'He sighs, shaking his head.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 5, 74, 0, ' How do so many people keep ending up in these cargo shuttles...?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 5, 75, 0, 'It is not that difficult, just slap the robots and open the cargo box.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 5, 76, 1, 'Certainly! Just let me get right on that...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 5, 77, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 6, 78, 0, 'The man taps his foot impatiently.', 1439635555);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 6, 79, 0, ' Well? I\'m still waiting.', 1439635555);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 6, 80, 1, 'Goodbye', 1439635555);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 7, 81, 0, 'How did it go?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 7, 82, 1, 'I\'ve done what you asked. Can you tell me who your contact is?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 7, 83, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 8, 84, 0, 'I\'ve had my fun watching you do my job. You didn\'t actually believe that I got a fake ID from someone here, did you? The only person I know around here is Marcus Stone, you could try asking him.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 8, 85, 1, 'I can see why you don\'t have too many friends.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 8, 86, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 9, 87, 0, 'Off you go, I need to relax now. It has been a long day at work.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536067, 6553, 9, 88, 1, 'Goodbye', 0);

-- Flint Novak
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 0, 89, 0, 'It\'s amazing how much trash finds itself on this planet. Hey, don\'t look at me like that, I was talking about the filth behind me.\\n\\nMy name is Flint. I look after the junkyard.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 0, 90, 1, 'Hi Flint, I\'m {name}. I was told you had work for me.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 0, 91, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 1, 92, 0, 'Nobody talks to me, the junkyard guy, unless they need something very specific, and usually something done off of the record. Funny how often these requests have been made recently...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 1, 93, 0, 'Flint takes a deep breath before continuing, shaking his head as though to clear the thoughts.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 1, 94, 0, 'Anyway, yes, first things first. I have a friend, yes, I\'ll call her a <i>friend</i> and she needs a small piece of hardware and... well, I <i>could</i> go do it myself, but...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 1, 95, 1, 'What hardware does she need and where could I find it?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 1, 96, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 2, 97, 0, 'You are a quick one, aren\'t you. Go out back to the \'yard and take out some of the old rickety robots back there. The device Alex needs is called a Bio Analyzing Computer. Destroy a few robots and you will find it. Don\'t bother coming back to me with it, give it to Alex directly.', 1439635564);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 2, 98, 1, 'What is a "Bio Analyzing Computer?"', 1439635564);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 2, 99, 1, 'What does this Alex person want with a piece of robot scrap?', 1439635564);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 2, 100, 1, 'Goodbye', 1439635564);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 3, 101, 0, 'Always with the questions...good workers used to just do what they were told.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 3, 102, 0, 'Flint sighs.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 3, 103, 0, '\\nThese gizmos works like brains for my robots. Originally, they were programmed to seek out and chase away any living creatures that wandered in, but now they all just move around aimlessly. That said they\'re a fairly generic part, good for all sorts of things. Alex gets me to procure one for her once in a while, for.... various reasons. Seems these little toys can be used for the construction of other things too.\\n\\nSo... be a pal and get this handled for me, won\'t you?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 3, 104, 1, 'What does this Alex person want with a piece of robot scrap?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 3, 105, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 4, 106, 0, 'Hey, be careful what you call "scrap" around here. That\'s MY scrap you\'re talkin\' about.\\n\\nAnyway, you\'ll just have to ask her. That\'s knowledge that I don\'t need to know or want to know. The less I know about her and her personal projects, the better.\\n\\nAnyway, it doesn\'t matter to you why she wants one - All that matters to you is that she does. Whatever you came here for, she should be able to help; Alex\'s a bit of a whiz. If you want more information on what she\'s going to do with it, ask her yourself. I\'m staying out of it.', 1439635570);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536068, 6553, 4, 107, 1, 'Goodbye', 1439635570);

-- Stanley Goodman
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 0, 108, 0, 'Hello there, stranger.\\n\\nThe name is Stan, and I am a man who likes to deal in previously-owned merchandise.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 0, 109, 1, 'I heard you could help me out... So, do you have any work for me?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 0, 110, 1, 'I have some questions.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 0, 111, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 1, 112, 0, 'My my... Aren\'t we eager? Ambition is something that is dreadfully lacking from new colonists, but... Oh dear. You have a <i>problem</i> don\'t you?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 1, 113, 1, 'I arrived here without an ID and Alex said you could help me...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 1, 114, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 2, 115, 0, 'You aren\'t the first one to wander through here with that particular issue as of late. As it so happens, you\'re in the right place and speaking to the right man... and if you can scratch my back, we\'ll see about relieving that itch on yours too.\\nThere\'s a... particular strongbox being kept in a small shack nearby. I <i>would</i> get it myself, but I only facilitate procurement, not enact. Get me whats in that box and I will help you out.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 2, 116, 1, 'I suppose I don\'t have much choice...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 2, 117, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 3, 118, 0, 'No, no you don\'t. Now, I\'ll be uploading the target\'s location to your NCU.\\n\\nThe work itself is relatively simple:\\n1: Buy a lockpick.\\n2: Sneak into the small building undetected. Locate the strongbox your NCU directs you toward and clean it out. Inside you\'ll find an Adaptation Factory.\\n3: Once you\'ve procurred this item, return to me and we\'ll see about creating your unique ID chip.', 1439586041);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 3, 119, 1, 'Goodbye', 1439586041);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 4, 120, 0, 'Did you need something?', 1439586046);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 4, 121, 1, 'I have some questions.', 1439586046);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 4, 122, 1, 'Goodbye', 1439586046);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 5, 123, 0, 'Watch your back!', 1439586049);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 5, 124, 0, 'Good to see you again... Did you find Antonio\'s Adaption Factory?', 1439586049);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 5, 125, 1, 'I think I have what you were looking for.', 1439586049);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 5, 126, 1, 'I have some questions.', 1439586049);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 5, 127, 1, 'Goodbye', 1439586049);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 6, 128, 0, 'Excellent, I was starting to get worried. I didn\'t want to deal with ICC and their incessant questioning again.\\n\\nLets go ahead and get at least part of your identification issue settled! I\'ll need the Nano Transmitter, the Blank Info Chip, and the Encryption Compiler and ofcourse the Adaptation Factory if you please.', 1439586048);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 6, 129, 0, 'He pockets the adaptation factory before turning his attention to the three pieces of your future ID chip, plugging them all together.', 1439586048);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 6, 130, 0, 'It would seem my instincts were correct about you. You\'re a natural. All it needs now is some additional information and security clearance codes and you should be ready to go. Visit Sarah Greene an \'associate\' of mine, and if you\'re smart you\'d do well to leave out my name. \\nAlso if you haven\'t done so already you should see my good friend Marco Spida and buy a Nanoprogram Container.', 1439586048);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 6, 131, 1, 'Thanks, it was fun.', 1439586048);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 6, 132, 1, 'I can\'t believe I just did that.', 1439586048);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536069, 6553, 6, 133, 1, 'Goodbye', 1439586048);

-- ICC Immigration Officer Bill
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 0, 134, 0, 'There have been a number of accidents in my shuttleport recently. I am told they are just that, accidents. I have lived here long enough to know that there are no accidents on this planet. These are actions of terrorists...some probably aren\'t even documented citizens.\\n\\nSpeaking of which, who are you?', 1439586026);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 0, 135, 1, 'Alex Gibbs thought you may be interested in seeing this?', 1439586026);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 0, 136, 1, 'My name is {name}. I am new to the planet.', 1439586026);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 0, 137, 1, 'Goodbye', 1439586026);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 1, 138, 0, 'Oh, please, the SecTec Monitor! Give it here at once! I have been waiting for this to arrive.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 1, 139, 0, 'This is just what she promised...oh wait...I am hearing something from Calitri\'s office.\\n\\nIt seems he wants to arrange an "accident" to befall one of the head workers, Cedric Harding. This is awful! I...I don\'t know what to do! I can\'t move forces, and by the time I find a superior officer, it could be all over.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 1, 140, 1, 'I will take care of it.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 1, 141, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 2, 142, 0, 'By yourself? That sounds dangerous.. Well, In that case you should take care of that guy he was talking to, "Alfonzo Rizzolo."\\n\\nOh and by the way, I don\'t know you and I have not heard anything about these plans. Don\'t talk to me again, got it?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536070, 6553, 2, 143, 1, 'Goodbye', 0);

-- Vernon Godfray
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 0, 144, 0, 'You. Take these.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 0, 145, 0, 'His outstretched hand offers you the two objects: One a small pill-sized object with electrical connectors poking from the sides, and the other some fashion of scan and input device. He pulls another object from his pocket with his free hand, a strangely green metallic tube. He waits for you to follow his instructions.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 0, 146, 1, 'Fine, I will hold the items for you.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 0, 147, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 1, 148, 0, 'As you take the two objects in-hand the man begins running his fingers along the tube, pressing at various points on its featureless, smooth surface. As he works a glow begins eminating from within, steadily growing brighter and brighter. After a moment he pauses and looks up to you for the first time, an expectant expression on his face.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 1, 149, 0, '... Well?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 1, 150, 1, '... well what?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 1, 151, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 2, 152, 0, 'Well get to it!', 1439586079);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 2, 153, 0, 'He seems less than pleased that you haven\'t done... whatever it is he wanted you to do by now.', 1439586079);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 2, 154, 0, 'The little thing is an Omni-Tek Technical Library. The scope thing is a Hacker Tool. I want the Library hacked because that will remove that absolutely moronic "Omni-Tek Employee Lock" on the damnable thing. Just use the tool on the library and get back to me when you\'re done.', 1439586079);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 2, 155, 0, 'With that he returns his attention to the glowing tube.', 1439586079);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 2, 156, 1, 'I\'m done now.', 1439586079);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 2, 157, 1, 'Goodbye', 1439586079);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 3, 158, 0, 'Are you now...', 1439586080);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 3, 159, 1, 'I was able to hack the device.', 1439586080);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 3, 160, 1, 'On second thought, I need some more time.', 1439586080);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 3, 161, 1, 'Goodbye', 1439586080);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 4, 162, 0, 'Without even blinking an eye the man continues to poke at the tube in his hands, nodding impassively.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 4, 163, 0, 'Very well, leave it here if you would.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 4, 164, 0, 'Well then, you seem like you are the useful kind of stray dog. Don\'t tell me, you are here to get my help with an ID card, but I won\'t do it for free. I have a simple task, and you\'ll need that Hacker Tool again. In the dock area, there\'s a Shipping Manifest Terminal. I need that terminal... altered. You can either go in via the freight entrance or the personnel entrance. Either way is up to you. Once you\'re in, find the terminal and use your Hacker Tool on it to reroute some of the data over to one of my... organization\'s own databases. Fairly simple, and once you\'re done I should be able to give you a hand. \\nDo you have any questions?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 4, 165, 1, 'Is this... legal?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 4, 166, 1, 'Nope.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 4, 167, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 5, 168, 0, 'Technically speaking, no. But neither is getting a fake ID.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 5, 169, 0, 'With that the man nods and returns his attention to the glowing green tube nearby, seemingly forgetting about your presence once more. You suppose question and answer time is over...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 5, 170, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 6, 171, 0, 'And you finally return. I was starting to think I\'d have to head on over to the docks and fish you out of some trouble.', 1439586087);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 6, 172, 1, 'I\'ve returned. The terminal was hacked.', 1439586087);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 6, 173, 1, 'Goodbye', 1439586087);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 7, 174, 0, 'Good. You should have an ID chip of some fashion. Give it here.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 7, 175, 0, 'You procure the chip from your pocket, the man quickly snatching it from your grip. He produces his own hacker tool and sets to work, holding the scope end over the small chip as he taps against the interface display. After only a few seconds he nods again and hands it to you once more.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 7, 176, 0, 'There. I\'ve programmed your chip with official ICC clearance codes. Same as if you\'d gotten them from a bureaucrat yourself. You\'ll need that for authentication purposes. Doctor Mason over at the hospital can help you out with imprinting your DNA in to the chip.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 7, 177, 1, 'Great! Anything else?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536072, 6553, 7, 178, 1, 'Goodbye', 0);

-- Sarah Greene
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 0, 179, 0, 'Well howdy, y\'all. Welcome and step on up to Sarah Greene\'s Armor Imporium. As a new arrival you may not know that it\'s dangerous to go alone through the wilds of Rubi-Ka, without proper protection. Lucky for you, I\'ve got the best custom-made protection available in the shuttleport!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 0, 180, 1, 'I heard that we might be able to help each other out...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 0, 181, 1, 'I have some questions.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 0, 182, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 1, 183, 0, 'You spend a few moments explaining your basic plight, making sure to avoid mentioning Stan\'s name.', 1439586052);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 1, 184, 0, 'I can help ya out. You got good timin\' too, just came up on my own little crisis. Somebody done made off with one of my prototype suits I was workin\' on. Ain\'t much they can <i>do</i> with it, bein\' DNA-locked to me and all, but it\'s still a pretty significant amount of effort and time from my end that went into it so I ain\'t quite ready to just let it go that easy. You get this back for me and I\'ll see what I can do about helping with your own issues.', 1439586052);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 1, 185, 1, 'Have you tried to contact ICC?', 1439586052);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 1, 186, 1, 'Goodbye', 1439586052);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 2, 187, 0, 'Sarah\'s eyes roll as she scoffs, head shaking side to side with unhidden disdain.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 2, 188, 0, 'That ain\'t gonna do me no good. Local patrols don\'t exactly have the best record of catchin\' thieves; tell the truth I can\'t think of a single time that one of us vendors got ripped off and we got our stuff back, regardless of affiliation or standin\'. Guess it just all comes with the territory.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 2, 189, 1, 'What do you have for sale?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 2, 190, 1, 'Do you know anyone I should talk to around here?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 2, 191, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 3, 192, 0, 'I offer several types of armor, some which aren\'t too common, and some which you won\'t find anywhere else. Come check out my wares.\\n\\n<font color="#ff0000">Press the Shopping Cart icon towards the bottom of the chat window to engage trade with Sarah.</font>', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 3, 193, 1, 'Do you know anyone I should talk to around here?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 3, 194, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 4, 195, 0, 'You should talk to Remi Gallois. He ain\'t such a bad guy.', 1439586063);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 4, 196, 1, 'Goodbye', 1439586063);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 5, 197, 0, 'Come back some time n\' check out my wares.', 1439586064);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 5, 198, 0, 'Well howdy, y\'all. Welcome and step on up to Sarah Greene\'s Armor Imporium. As a new arrival you may not know that it\'s dangerous to go alone through the wilds of Rubi-Ka, without proper protection. Lucky for you, I\'ve got the best custom-made protection available in the shuttleport!', 1439586064);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 5, 199, 1, 'I found the stolen armor.', 1439586064);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 5, 200, 1, 'I have some questions.', 1439586064);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 5, 201, 1, 'Goodbye', 1439586064);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 6, 202, 0, 'Sarah claps her hands happily, beaming as you produce the stolen suit.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 6, 203, 0, ' And you actually even brought it back! Well hot damn. If you don\'t mind passin\' it over...?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 6, 204, 0, 'Yeeehaw, y\' found it! I was startin\' to think it was lost an\' gone forever. I\'m so happy I could kiss ya! But I won\'t, \'cause that\'s a little creepy.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 6, 205, 0, 'With a small \'thud\' she tosses a package onto the counter in front of you, heavy and thick.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 6, 206, 0, 'There ya go, a deal is a deal.\\nI\'m sure Vernon Godfray should be able to help with aquiring more parts needed for your ID card.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 6, 207, 1, 'Thanks.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 6, 208, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 7, 209, 0, 'Good luck out there.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536073, 6553, 7, 210, 1, 'Goodbye', 0);

-- Shipping Manifest Terminal
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 0, 211, 0, 'Please choose an operation:\\n* Register a new/completed shipment\\n* Cancel an existing shipment\\n* Access Shipping Manifest Logging', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 0, 212, 1, '(Access Shipping Manifest Logging)', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 0, 213, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 1, 214, 0, 'Access Denied.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 1, 215, 1, '(Apply Hacker Tool)', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 1, 216, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 2, 217, 0, 'Please Enter Password.', 1439586083);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 2, 218, 0, 'Password Correct. Access Granted.\\n\\nShipping Manifest Logs are maintained within ICC Headquarters. To view Log History, please schedule an appointment with Roy K. of Shuttleport Administration.', 1439586083);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 2, 219, 1, '(Re-route shipping report data.)', 1439586083);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 2, 220, 1, 'Goodbye', 1439586083);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 3, 221, 0, 'Updating', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 3, 222, 0, '', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 3, 223, 0, '', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 3, 224, 0, '', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 3, 225, 0, '\\n\\nNew data delay point has been specified. No notification messages have been sent.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536074, 6553, 3, 226, 1, 'Goodbye', 0);

-- Lorelei the Bartender
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 0, 227, 0, 'The young woman before you smiles brightly as you approach, her hands busy polishing a tumbler with a rather less-than-sanitary looking rag.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 0, 228, 0, 'Hey there, cutie. What can I get ya?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 0, 229, 1, 'I was told you might be able to give me a hand...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 0, 230, 1, 'What do you have?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 0, 231, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 1, 232, 0, 'You yet again start the process of explaining your situation when, barely five words in, Lorelei raises her hand to stop you with a soft, friendly chuckle.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 1, 233, 0, 'No, no, it\'s ok I can help you! You don\'t have to go through the whole story. Probably better I <i>don\'t</i> know. However, I will need you to do me a favor before I help you out.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 1, 234, 1, 'I will help you, what do you need?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 1, 235, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 2, 236, 0, 'You need to find my stupid Reet, Lolly. He escaped again... I believe he might have found a girlfriend or something... Try looking for him near the other Reets in the desert oasis.', 1439586115);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 2, 237, 1, 'What is a Reet?', 1439586115);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 2, 238, 1, 'Is there anything in particular I should know about Lolly?', 1439586115);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 2, 239, 1, 'Goodbye', 1439586115);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 3, 240, 0, 'Raising an eyebrow she looks slightly confused for a moment.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 3, 241, 0, 'Its a bird! Have you been living under a rock?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 3, 242, 1, 'Is there anything in particular I should know about Lolly?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 3, 243, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 4, 244, 0, 'The young woman before you smiles brightly as you approach, her hands busy polishing a tumbler with a rather less-than-sanitary looking rag.', 1439635506);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 4, 245, 0, 'Hey there, cutie. What can I get ya?', 1439635506);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 4, 246, 1, 'I have the bird...', 1439635506);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 4, 247, 1, 'What do you have?', 1439635506);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 4, 248, 1, 'Goodbye', 1439635506);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 249, 0, 'With a sharp smile and nod the bartender extends her hand.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 250, 0, 'Alright then, sounds like we\'re ready to do this. I\'ll need the ID chip too.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 251, 0, 'She places the bird cage on the ground and you can see Lolly breaking out of his cage as she crouches down behind the counter.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 252, 0, 'This\'ll take just a sec.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 253, 0, 'You hear a few miscellaneous beeps and dings from... something... underneath the bar top.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 254, 0, ' And here you go. Congratulations, you are now a fully-fledged colonist of Rubi-Ka!\\nYou should talk to Vaughn Hammond about entering ICC HQ.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 255, 1, 'Thank you.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 5, 256, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 6, 257, 0, 'The young woman before you smiles brightly as you approach, her hands busy polishing a tumbler with a rather less-than-sanitary looking rag.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 6, 258, 0, 'Hey there, cutie. What can I get ya?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 6, 259, 1, 'What do you have?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536075, 6553, 6, 260, 1, 'Goodbye', 0);

-- Dr. Mason
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 0, 261, 0, 'Good day to you! If you\'ve come looking to improve your quality of life through the power of bodily augmentation, you have come to the right place.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 0, 262, 1, 'I heard you might be able to help me...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 0, 263, 1, 'I have some questions...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 0, 264, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 1, 265, 0, 'You spend a few moments explaining your situation to the patient doctor.', 1439586090);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 1, 266, 0, 'Ah, yes, another one. I should say that forging an ID chip is rather illegal and somewhat risky... but truthfully I\'d rather lend a hand against the bureaucratic red-tape-nonsense than be a stickler for the law. \\n\\nHowever, I won\'t help you without a little hand in return. Trust me, the task I have for you is mutually beneficial.. ', 1439586090);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 1, 267, 0, 'See my fine selection of vending machines? ICC wants me to make more money, and that is where you come in. Assemble a Leg Implant: Agility, Shiny, I will upload the recipe to your ncu. Show it to me when you are done.', 1439586090);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 1, 268, 1, 'I\'ll come back when I\'m done.', 1439586090);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 1, 269, 1, 'Goodbye', 1439586090);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 2, 270, 0, 'Sounds good. I\'ll be here.', 1439586106);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 2, 271, 1, 'Goodbye', 1439586106);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 3, 272, 0, 'Welcome back, I hope you were successful.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 3, 273, 1, 'I finished that implant.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 3, 274, 1, 'I have some questions...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 3, 275, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 4, 276, 0, 'Good, let\'s see how you did. Please give it to me.', 1439586112);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 4, 277, 0, 'Not bad for a beginner. Now, I think you should install it. Use the Surgery Clinic in the corner.', 1439586112);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 4, 278, 1, 'How do I install an Implant?', 1439586112);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 4, 279, 1, 'Goodbye', 1439586112);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 5, 280, 0, 'Welcome back, I hope you were successful.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 5, 281, 1, 'I have installed the implant and I have all the items we need for the chip.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 5, 282, 1, 'I have some questions...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 5, 283, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 6, 284, 0, 'The good doctor clasps his hands together with a big smile, nodding to himself.', 1439586114);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 6, 285, 0, 'Excellent, then let\'s get started. I\'ll need the Blank ICC ID Chip and the Biological Survey Nanobots, if you please.', 1439586114);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 6, 286, 0, 'Without any warning he pokes the side of your neck with the DNA-Unlocking device. A little droplet of blood runs down the outside of your throat.', 1439586114);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 6, 287, 0, 'And... there we have it, one personalized ID chip, ready to be used in a card. \\nLorelei down at the bar has a... slight side business. Speak with her, and she should be able to help out with the final step.', 1439586114);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 6, 288, 1, 'Thank you for all the help.', 1439586114);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536076, 6553, 6, 289, 1, 'Goodbye', 1439586114);

-- Vaughn Hammond
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 0, 290, 0, 'A low sigh comes from inside the Peackeeper\'s helmet.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 0, 291, 0, 'Hello citizen, on behalf of the ICC, I welcome you to Rubi-Ka.\\n\\nPlease present your identification to proceed to the elevator.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 0, 292, 1, 'Here\'s my identification.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 0, 293, 1, 'I have some questions.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 0, 294, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 1, 295, 0, 'Very well then, let\'s see it.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 1, 296, 0, 'Everything...seems to be in order. I have uploaded your identity details to your NCU. You may leave Arete Landing whenever you are ready.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 1, 297, 1, 'How do I leave?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 1, 298, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 2, 299, 0, 'Hello again, citizen. Are you prepared to depart from Arete Landing?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 2, 300, 1, 'I have some questions.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 2, 301, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 3, 302, 0, 'Let me hear them.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 3, 303, 1, 'What is ICC?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 3, 304, 1, 'Who are you?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 3, 305, 1, 'Why do you need an ID to leave this place?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 3, 306, 1, 'What will I find in when I leave Arete Landing?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536083, 6553, 3, 307, 1, 'Goodbye', 0);

-- Remi Gallois
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536085, 6553, 0, 308, 0, 'Flashing a killer smile as you approach the young man before you spreads his arms at his side, directing attention to his wares.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536085, 6553, 0, 309, 0, 'This is a dangerous world, friend - I\'d hope you\'re prepared. That Omni-Tek... yokel over there believes that you can hide from your troubles behind some shiny plasteel. Personally, I prescribe to the idea that the best offense? Is a damn good offense.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536085, 6553, 0, 310, 1, 'Do you sell any weapons?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536085, 6553, 0, 311, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536085, 6553, 1, 312, 0, 'Weapons are what I live and breathe. Check out what I have, you won\'t be dissapointed.\\n\\n', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536085, 6553, 1, 313, 0, '<font color="#ff0000">Press the button below marked with the symbol of a Shopping Cart. It will give you an overview of the current stock.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536085, 6553, 1, 314, 1, 'Goodbye', 0);

-- Antonio Stacklund
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536092, 6553, 0, 315, 0, 'Hello there, welcome to Rubi-Ka! Looking for an upgrade for your weapon? With my greatest creation, the Adaptation Factory, you can improve your old, worn weapon!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536092, 6553, 0, 316, 1, 'As a matter of fact I would like to upgrade my weapon...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536092, 6553, 0, 317, 1, 'Do you have any weapons to sell?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536092, 6553, 0, 318, 1, 'Do you only have weapons and weapon upgrades?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536092, 6553, 0, 319, 1, 'Goodbye', 0);

-- Marco Spida
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536097, 6553, 0, 320, 0, 'Do you need any Nano Programs? I\'m the guy.', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536097, 6553, 0, 321, 1, 'What can you tell me about Nano Programs?', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2052536097, 6553, 0, 322, 1, 'Goodbye', 0);

-- Lolly the Reet
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 0, 323, 0, 'Lolly wants a cracker!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 0, 324, 1, 'Please get in to the cage...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 0, 325, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 1, 326, 0, 'U giev cracker! Nao!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 1, 327, 0, 'The bird starts eating the tasty cookie. It is distracted, now is the time to catch it!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 1, 328, 1, '(Quietly pick up the bird)', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 1, 329, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 2, 330, 0, 'Lolly doesn\'t notice that you get closer and by the time he does, he is already in your hands!', 1439635504);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054140095, 6553, 2, 331, 1, 'Goodbye', 1439635504);

-- Lolly the Reet
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 0, 332, 0, 'Lolly wants a cracker!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 0, 333, 1, 'Please get in to the cage...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 0, 334, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 1, 335, 0, 'U giev cracker! Nao!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 1, 336, 0, 'The bird starts eating the tasty cookie. It is distracted, now is the time to catch it!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 1, 337, 1, '(Quietly pick up the bird)', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 1, 338, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 2, 339, 0, 'Lolly doesn\'t notice that you get closer and by the time he does, he is already in your hands!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054141801, 6553, 2, 340, 1, 'Goodbye', 0);

-- Lolly the Reet
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 0, 341, 0, 'Lolly wants a cracker!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 0, 342, 1, 'Please get in to the cage...', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 0, 343, 1, 'Goodbye', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 1, 344, 0, 'U giev cracker! Nao!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 1, 345, 0, 'The bird starts eating the tasty cookie. It is distracted, now is the time to catch it!', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 1, 346, 1, '(Quietly pick up the bird)', 0);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 1, 347, 1, 'Goodbye', 0);

-- OmniCell-owned actions attached to captured item-handover sentences. The
-- sentence text and quest association are capture-derived; the Action value
-- is emulator behavior because the retail answer-action field is not decoded.
-- Runtime state filtering offers these only while the quest wants its item.
UPDATE knubotscript SET Grants = 1439586025, Action = 3
 WHERE Npc = 2052536070 AND Playfield = 6553 AND Ordinal = 135;
UPDATE knubotscript SET Grants = 1439586046, Action = 3
 WHERE Npc = 2052536069 AND Playfield = 6553 AND Ordinal = 125;
UPDATE knubotscript SET Grants = 1439586063, Action = 3
 WHERE Npc = 2052536073 AND Playfield = 6553 AND Ordinal = 199;
UPDATE knubotscript SET Grants = 1439586079, Action = 3
 WHERE Npc = 2052536072 AND Playfield = 6553 AND Ordinal = 159;
UPDATE knubotscript SET Grants = 1439635504, Action = 3
 WHERE Npc = 2052536075 AND Playfield = 6553 AND Ordinal = 246;
UPDATE knubotscript SET Grants = 1439586112, Action = 3
 WHERE Npc = 2052536076 AND Playfield = 6553 AND Ordinal = 281;
UPDATE knubotscript SET Grants = 1439635570, Action = 3
 WHERE Npc = 2052536065 AND Playfield = 6553 AND Ordinal = 1;

-- OmniCell-authored fallback sentences: captured walks did not contain a
-- selectable sentence at these two required handover points. Their text and
-- Action values are emulator decisions and are not attributed to retail.
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants, Action)
 VALUES (2052536072, 6553, 7, 10001, 1, 'Here is the identification chip.', 1439586083, 3);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants, Action)
 VALUES (2052536066, 6553, 4, 10002, 1, 'Here is the fire suppressant container.', 1439635562, 3);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 2, 348, 0, 'Lolly doesn\'t notice that you get closer and by the time he does, he is already in your hands!', 1439635504);
INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants) VALUES (2054564628, 6553, 2, 349, 1, 'Goodbye', 1439635504);
