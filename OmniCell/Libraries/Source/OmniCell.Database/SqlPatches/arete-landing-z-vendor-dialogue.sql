-- Conversations of two Arete Landing vendors, kept from the earlier AreaExtract dialogue patch (captured lines,
-- no emulator-owned actions). No capture QuestExtract reads has them, so arete-landing-quests.sql - which
-- deletes every knubotscript row of the playfield - cannot generate them. Runs after it.

DELETE FROM knubotscript WHERE Playfield = 6553 AND Npc IN (2052536085, 2052536092);

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
