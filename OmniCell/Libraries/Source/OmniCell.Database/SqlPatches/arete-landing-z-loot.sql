-- Arete Landing loot represented by emulator-owned database rules.
--
-- Capture evidence:
--   * Alex Gibbs says Robot Junk is looted from robot remains and is a chance.
--   * The captured item is Robot Junk 42620/42620 at QL 1.
--   * The ordinary full-bodied robots are named exactly "32-V Docker".
--
-- The captures do not expose the server's probability.  The 25% value below
-- is a new OmniCell gameplay rate, deliberately defined here rather than
-- attributed to the live server.  One eligible kill therefore has one roll.

DELETE FROM moblootprofiles
WHERE Playfield = 6553
  AND MobName = '32-V Docker'
  AND DropHash = 'ARETE_DOCKER_ROBOT_JUNK';

DELETE FROM mobdroptable
WHERE Hash = 'ARETE_DOCKER_ROBOT_JUNK';

INSERT INTO mobdroptable
    (Hash, LowId, HighId, MinQl, MaxQl, RangeCheck)
VALUES
    ('ARETE_DOCKER_ROBOT_JUNK', 42620, 42620, 1, 1, 0);

INSERT INTO moblootprofiles
    (Playfield, MobName, DropHash, Rolls, Chance)
VALUES
    (6553, '32-V Docker', 'ARETE_DOCKER_ROBOT_JUNK', 1, 2500);
