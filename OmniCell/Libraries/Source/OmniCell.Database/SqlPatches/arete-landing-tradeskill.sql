-- The Arete Landing tradeskill tutorial, Tradeskilling (1/4) to (4/4).
--
-- These four combines are missing from the tradeskill table. It holds 108783
-- recipes, but none for these items: its Id2 values start at 101102 and Robot
-- Junk is 42620, so whatever rdbreader read them out of did not carry the
-- newbie tutorial. The item names are all present in itemnames, so this is a
-- gap in the recipes rather than in the items.
--
-- Captured mission text proves these source/target/result pairs, including the
-- low/high item template identities and the source/target order:
--
--   1/4  Screwdriver 150922 + Robot Junk 42620            = Nano Sensor 150923
--   Hacking Skills
--        Hacker Tool 87810 + Omni-Tek Technical Library 248377
--                                                           = Hacked Technical
--                                                             Library 295756
--
-- The wire capture also proves Screwdriver 150922 is QL 10, Robot Junk 42620
-- is QL 1, and Hacked Technical Library 295756 and Hacker Tool 87810 are QL 1.
--
-- The intermediate 2/4 and 3/4 formulas below are required to make the quest
-- chain runnable, but the available captures do not contain those two mission
-- stages or builds. Their formula pairing remains UNVERIFIED; do not cite it as
-- captured evidence. Their low/high identities do follow the itemref pairs and
-- the receiver's documented high-ID lookup contract.
--
-- Two things here are OmniCell-owned implementation decisions rather than
-- captured retail values:
--
-- Skill requirement. The quest text says the mission "will help you learn the
-- basics" and warns only that Engineers and Traders have profession tools to
-- help. OmniCell sets the named mechanical and electrical engineering skills
-- to 0 percent so the proof-of-concept tutorial is not gated. The captures do
-- not prove the retail threshold.
--
-- DeleteFlag 3 consumes source and target. The quest presents A + B = C, but
-- no available capture proves the retail DeleteFlag value.

-- tradeskill has no primary or unique key. Delete both the current high-ID
-- keys and the obsolete low-ID keys so repeated imports cannot leave duplicate
-- or contradictory recipes loadable.
DELETE FROM tradeskill
WHERE (Id1=150922 AND Id2=42620)
   OR (Id1=156020 AND Id2=150923)
   OR (Id1=156021 AND Id2=150924)
   OR (Id1=156024 AND Id2=156022)
   OR (Id1=156025 AND Id2=156023)
   OR (Id1=87810 AND Id2=248377);

INSERT INTO tradeskill
  (Id1, Id2, MinTarget, ResultIds, QlRangePercent, DeleteFlag, Skill, SkillPercent, SkillPerBump, MaxBump, MinXP, MaxXP, IsImplant)
VALUES
  (150922, 42620,  0, '150923,150924', 100, 3, '125,126', '0,0', '0,0', 0, 0, 0, 0),
  (156021, 150924, 0, '156022,156023', 100, 3, '125,126', '0,0', '0,0', 0, 0, 0, 0),
  (156025, 156023, 0, '156026,156027', 100, 3, '125,126', '0,0', '0,0', 0, 0, 0, 0),
  (87810, 248377, 0, '295756,295756', 100, 3, '125,126', '0,0', '0,0', 0, 0, 0, 0);
