-- The merged captures contain replacement identities for the same transient
-- Gas Fire fixtures. Arete has four fires; keep the four captured canonical
-- identities instead of spawning every snapshot on top of the landing pad.
DELETE FROM staticdynels
WHERE Playfield = 6553
  AND Instance IN (
      1477281898, 1477282328, 1477282668, 1477282740, 1477282826,
      1477282978, 1477282979, 1477282980, 1477283021, 1477283022,
      1477283023, 1477283035, 1477283046, 1477283068, 1477283082,
      1477283108, 1477283109, 1477283110);
