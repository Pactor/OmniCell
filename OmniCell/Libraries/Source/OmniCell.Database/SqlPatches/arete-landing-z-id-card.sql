-- The ID card, which is what the whole Arete Landing chain is for.
--
-- Arete's exit is three statels: the "Exit Arete Landing" console, a door beside
-- it, and playfield 655 on the other side. Both halves of the console and the
-- door's own behaviour ask the same thing - bit 16384 of stat 685 - and the
-- console's third line is what you get without it:
--
--   OnUse: CastNano(295602)                   [Self stat 685 BitAnd 16384]
--          Teleport(3364, 18, 835, 0)         [Self stat 685 BitAnd 16384]
--          SystemText("You need an ID card to enter ICC HQ.")
--                                             [Self stat 685 NotBitAnd 16384]
--
-- So bit 16384 is the card, and the Identity Crisis chain is the making of it:
-- every stage's description says so, the stage before last hands over item
-- 296692 "Identification Card", and the last one - "Talk to Vaughn Hammond" -
-- is about leaving.
--
-- What sets the bit in retail is not captured. Every capture we hold is of a
-- character who had already left: one recorded inside an Arete instance reads
-- stat 685 = 16384 exactly, and characters out on Rubi-Ka read 28668, which
-- contains it. Nothing shows the moment it is granted.
--
-- OmniCell-defined: it is granted by finishing the last quest of the chain.
-- That is the one whose own text is "Your ID card is finally complete! Talk to
-- Vaughn Hammond about leaving Arete Landing", so a player who has done the
-- work can leave and one who has not is told to find an ID card, which is the
-- message the console already has for them. Granting it with the card itself,
-- one quest earlier, would let the last stage be skipped.
--
-- The bit is ored in rather than written, because stat 685 is a flag word - a
-- character out on Rubi-Ka carries eleven more bits in it - and writing would
-- take away whatever else is there.

-- The table is in SqlTables and a fresh database has it, but a database made
-- before 2026-09-27 does not and would fail on the next line.
CREATE TABLE IF NOT EXISTS `queststatrewards` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `QuestId` int(32) NOT NULL,
  `Stat` int(32) NOT NULL,
  `Value` int(32) NOT NULL DEFAULT 0,
  `SetBits` int(1) NOT NULL DEFAULT 1,
  `GrantOnAccept` int(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `QuestId` (`QuestId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DELETE FROM queststatrewards WHERE QuestId = 1439635506;

INSERT INTO queststatrewards (QuestId, Stat, Value, SetBits, GrantOnAccept)
VALUES (1439635506, 685, 16384, 1, 0);
