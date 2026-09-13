-- Reachable quest controls for every Arete mission.
--
-- Captures contain only the dialogue branches the recorded player selected.
-- Those partial walks are useful dialogue content but cannot, by themselves,
-- guarantee that another player can select every mission transition. These
-- emulator-owned answers are placed in step zero (the step every conversation
-- opens on), and ScriptedKnuBot hides each one unless its state is applicable.

DELETE FROM knubotscript
WHERE Playfield = 6553
  AND Text LIKE 'I am ready to help with mission:%';

DELETE FROM knubotscript
WHERE Playfield = 6553
  AND Text LIKE 'I have completed mission:%';

DELETE FROM knubotscript
WHERE Playfield = 6553
  AND Text LIKE 'I have the requested items for mission:%';

-- OmniCell fallback acceptance is attached to the NPC association stored in
-- the converted quest row. That association is capture-backed when the quest
-- appeared during an open conversation and explicitly approximate otherwise;
-- this generic fallback is not claimed to be a captured retail sentence.
INSERT INTO knubotscript
    (Npc, Playfield, Step, Ordinal, Kind, Text, Grants, Action)
SELECT q.GiverId,
       q.Playfield,
       0,
       1600000000 + MOD(q.Id, 100000000),
       1,
       CONCAT('I am ready to help with mission: ', q.Name),
       q.Id,
       1
FROM quests q
WHERE q.Playfield = 6553;

-- Item deliveries use the NPC attached to the captured hand-in sentence by
-- the earlier emulator action mapping. The same trade box handles every
-- HandIn objective on that quest. The generated sentence is OmniCell-authored.
INSERT INTO knubotscript
    (Npc, Playfield, Step, Ordinal, Kind, Text, Grants, Action)
SELECT MIN(k.Npc),
       q.Playfield,
       0,
       1700000000 + MOD(q.Id, 100000000),
       1,
       CONCAT('I have the requested items for mission: ', q.Name),
       q.Id,
       3
FROM quests q
JOIN questobjectives o
  ON o.QuestId = q.Id
 AND o.ObjectiveType = 7
JOIN knubotscript k
  ON k.Playfield = q.Playfield
 AND k.Grants = q.Id
 AND k.Action = 3
WHERE q.Playfield = 6553
GROUP BY q.Id, q.Playfield, q.Name;

-- Talk missions finish at the character they name. Other non-item missions
-- are reported back to their giver. Hand-in missions complete in their trade
-- action and deliberately receive no second reward action.
INSERT INTO knubotscript
    (Npc, Playfield, Step, Ordinal, Kind, Text, Grants, Action)
SELECT CASE
           WHEN o.ObjectiveType = 3 THEN MIN(target.Id)
           ELSE q.GiverId
       END,
       q.Playfield,
       0,
       1800000000 + MOD(q.Id, 100000000),
       1,
       CONCAT('I have completed mission: ', q.Name),
       q.Id,
       2
FROM quests q
JOIN questobjectives o ON o.QuestId = q.Id
LEFT JOIN mobspawns target
  ON target.Playfield = q.Playfield
 AND target.Name = o.Target
WHERE q.Playfield = 6553
  AND NOT EXISTS (
      SELECT 1
      FROM questobjectives handin
      WHERE handin.QuestId = q.Id
        AND handin.ObjectiveType = 7)
GROUP BY q.Id, q.Playfield, q.Name, q.GiverId, o.ObjectiveType;
