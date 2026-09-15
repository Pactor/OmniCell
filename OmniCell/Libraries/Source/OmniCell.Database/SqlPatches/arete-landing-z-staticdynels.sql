-- The merged captures contain replacement identities for the same transient
-- Gas Fire fixtures; keep the captured canonical identities instead of
-- spawning every snapshot on top of the landing pad.
--
-- Retail has nine Gas Fire positions around Marcus Stone's platform, the same
-- nine in every retail session (follow_*, srv_ao_*, t_ao_* and the 2026-09-14
-- quest sniffs); five are sent on zone entry and four more as the player walks
-- up. A fire's instance id is not stable: an extinguished fire despawns and is
-- relit at the same position under a new id (20260914-124401 seq 2154 -> 2833).
-- So the canonical set is one fixture per position, and a quest must match a
-- fire by what it is, never by its id:
--   1477281898 3584,819   1477282328 3599,844   1477283068 3602,842
--   1477021836 3607,821   1477282740 3608,841   1477283092 3630,832
--   1477283083 3634,835   1477282826 3636,846 (on top of the tent, highest)
--   1477283084 3640,833
-- This used to keep only four of them, leaving the tent fire and four others
-- missing in game. Deleted below: the other snapshots of the same positions.
DELETE FROM staticdynels
WHERE Playfield = 6553
  AND Instance IN (
      1477282668,
      1477282978, 1477282979, 1477282980, 1477283021, 1477283022,
      1477283023, 1477283035, 1477283046, 1477283082,
      1477283108, 1477283109, 1477283110);
