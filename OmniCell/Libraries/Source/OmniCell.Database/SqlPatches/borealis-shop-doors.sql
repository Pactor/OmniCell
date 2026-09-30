-- Where the two Borealis shop doors put you down, corrected from the
-- 2026-09-29 recordings.
--
-- Funcom condensed the shops years ago: one interior playfield is shared by
-- many cities, and which door you arrive at - and leave by - depends on the
-- playfield you came from. Playfield 1186 has seven doors and eight cities
-- enter it. So a row here is only correct for the city it belongs to, and
-- only the two Borealis ones were recorded.
--
-- Stream 8 of that day is Fair Trade. Its PlayfieldAnarchyF puts the player
-- down at 175.01, 5.01, 109.81 - on the door at 175, 5, 108, which is statel
-- 3221488802 - and its closing N3Teleport leaves by the same door, back to
-- playfield 800. The table said 3221226658 at 198, 5, 125, which is a
-- different part of the building.
--
-- It is not only where you appear. PlayfieldLoader hangs the
-- ExitProxyPlayfield on whichever door a row names, so naming the wrong one
-- put the way out under your feet on arrival: walk forward out of the wing
-- you woke up in and you were back outside.
--
-- The implant shop is the same fault at its own door, from stream 21.
--
-- The other six cities entering 1186, and the two entering 2073, are left
-- alone. Their doors are not in this recording and each one has its own.

UPDATE teleports SET destinationInstance = 3221488802
 WHERE playfield = 800 AND statelInstance = 3222733600 AND destinationPlayfield = 1186;

UPDATE teleports SET destinationInstance = 3221293081
 WHERE playfield = 800 AND statelInstance = 3221685024 AND destinationPlayfield = 2073;
