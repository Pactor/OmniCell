CREATE TABLE IF NOT EXISTS `knubotscript` (
  `Id` int(32) NOT NULL AUTO_INCREMENT,
  `Npc` int(32) NOT NULL,
  `Playfield` int(32) NOT NULL,
  `Step` int(32) NOT NULL,
  `Ordinal` int(32) NOT NULL,
  `Kind` int(32) NOT NULL DEFAULT 0,
  `Text` text NOT NULL,
  `Grants` int(32) NOT NULL DEFAULT 0,
  `Action` int(32) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `talker` (`Playfield`,`Npc`,`Ordinal`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- What a character says when you talk to it, and what the window offers you
-- back. Recovered from captures of the live server - see
-- Tools/Capture/AreaExtract - so every line of it is Funcom's.
--
-- A conversation is a sequence of steps. Within a step, Kind 0 rows are what
-- the character says, in Ordinal order, and Kind 1 rows are the answers the
-- client is offered. Action attaches a server operation to that exact answer.
-- Rows with Action 0 retain the captured convention: the last answer leaves
-- and any other answer moves to the next step.
--
-- Grants is the quest id used by an explicit Action, or the legacy quest that
-- changed hands at a captured step.
--
-- Empty by default. Arete Landing's rows are generated, not written by hand.
--
