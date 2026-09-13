-- OmniCell-owned representation of the quest-created Merchant's Strongbox.
-- It is absent from the available static world captures; those captures do
-- not prove why or when retail creates it. The quest provides marker 3410,893
-- and Strongbox template 295604 / 0x000482B4. Instance 900000001 and reuse of
-- the canonical six-stat wire shape are emulator implementation decisions,
-- not captured retail values.

DELETE FROM staticdynels WHERE Playfield = 6553 AND Instance = 900000001;

INSERT INTO staticdynels
    (Type, Instance, Playfield, X, Y, Z, HeadingX, HeadingY, HeadingZ, HeadingW, stats, customevents)
SELECT Type,
       900000001,
       Playfield,
       3410,
       9.01,
       893,
       0,
       0,
       0,
       1,
       UNHEX(REPLACE(HEX(stats), '0004833A', '000482B4')),
       NULL
FROM staticdynels
WHERE Playfield = 6553 AND Instance = 1477021842;
