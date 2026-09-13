-- OmniCell-owned quest item delivery rules needed to make the represented
-- Arete chain playable. Item identities and roles are transcribed from
-- captured quest text and converted item records. The available captures do
-- not prove the exact retail delivery packet/timing, so GrantOnAccept is an
-- emulator implementation decision, not a claim about retail behavior.
-- GrantOnAccept=1 supplies a tool for the objective being accepted. Zero
-- supplies the physical result of a completed mission for a later mission.

DELETE FROM questitemrewards
WHERE QuestId IN (SELECT Id FROM quests WHERE Playfield = 6553);

INSERT INTO questitemrewards (QuestId, ItemId, Quantity, GrantOnAccept) VALUES
  (1439586017, 295801, 1, 1), -- RC-P Audio Recording Device
  (1439586041, 248306, 1, 0), -- Antonio's Adaptation Factory
  (1439586052, 295618, 1, 0), -- DNA-Locked Prototype Raven Combat Armor
  (1439586067, 248377, 1, 1), -- Omni-Tek Technical Library to hack
  (1439586080, 296572, 1, 0), -- Unprogrammed Identification Chip
  (1439586083, 296575, 1, 0), -- Blank ICC ID Chip
  (1439586109, 296574, 1, 0), -- Biological Survey Nanobots
  (1439586115, 297367, 1, 0), -- Pet Cage With a Reet
  (1439635556, 296780, 1, 1), -- Compact Fire Suppressant Container
  (1439635564, 156020, 1, 0), -- Bio Analyzing Computer
  (1439635570, 295800, 1, 0); -- Rebuilt HC-12 SecTec Monitor
