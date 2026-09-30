-- Arete Landing's statel runs: the client instance ids its terminals and doors go by.
--
-- An instanced playfield names its statels to the client in PlayfieldAnarchyF, as runs over the
-- playfield file's own statel list. Retail sent Arete Landing five of them, identically in the
-- implant session and the Shade session (20260909-142713 s3 #2, 20260914-220505 s5 #2):
--
--   Door            file position 0        1 statel    from 280561673 (0x10B10809)
--   Terminal        file position 1        1 statel    from 1477021805 (0x5809906D)  Enter ICC HQ
--   VendingMachine  file positions 2-9     8 statels   from 320051221 (0x13139815)
--   Terminal        file positions 10-25  16 statels   from 1477021806 (0x5809906E)  Exit Arete Landing ...
--   Door            file position 26       1 statel    from 280561674 (0x10B1080A)
--
-- Every client id the captures show fits: Exit Arete Landing (position 10) 1477021806, Merchant's
-- Strongbox (21) 1477021817, Remains of Shop Thief (22) 1477021818, Surgery Clinic (24) 1477021820.
-- The quest objectives that name those ids keep matching because these are retail's own numbers.
--
-- The VendingMachine run is left out on purpose: OmniCell spawns Arete Landing's vendors itself
-- (arete-landing-vendors.sql), at the same spots, and sending the run as well would put a second
-- machine on top of each of them.

CREATE TABLE IF NOT EXISTS `playfieldstatelruns` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `Playfield` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL DEFAULT 0,
  `Type` int(32) NOT NULL,
  `StartIndex` int(32) NOT NULL,
  `Count` int(32) NOT NULL,
  `FirstInstance` int(32) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Playfield` (`Playfield`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DELETE FROM playfieldstatelruns WHERE Playfield = 6553;
INSERT INTO playfieldstatelruns (Playfield, Ordinal, Type, StartIndex, Count, FirstInstance) VALUES (6553, 0, 51016, 0, 1, 280561673);
INSERT INTO playfieldstatelruns (Playfield, Ordinal, Type, StartIndex, Count, FirstInstance) VALUES (6553, 1, 51005, 1, 1, 1477021805);
INSERT INTO playfieldstatelruns (Playfield, Ordinal, Type, StartIndex, Count, FirstInstance) VALUES (6553, 2, 51005, 10, 16, 1477021806);
INSERT INTO playfieldstatelruns (Playfield, Ordinal, Type, StartIndex, Count, FirstInstance) VALUES (6553, 3, 51016, 26, 1, 280561674);
