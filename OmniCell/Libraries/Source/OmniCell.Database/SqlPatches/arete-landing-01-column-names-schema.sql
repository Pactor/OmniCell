-- Columns renamed once their meaning was shown. SqlTables has the new names for a fresh database;
-- this renames them on an existing one, and does nothing the second time. It sorts after the 00-
-- schema patches (arete-landing-00-weapon-schema.sql adds a column after Unknown7) and before the
-- data patches that insert by the new names.
--
--   mobspawnsweapons.Unknown6  -> StaticInstance  stat 23, read and written as that stat
--   mobspawnsweapons.Unknown7  -> MultipleCount   stat 412, the same
--   questwire.Unknown21        -> RequiredCount   QuestInfo's count: 5 on "Terminate 5 ..."
--   questwirerewards.Unknown1  -> Unused          the ACGItem_t's reserved zero

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mobspawnsweapons' AND COLUMN_NAME = 'Unknown6') = 0,
  'SELECT 1',
  'ALTER TABLE mobspawnsweapons CHANGE COLUMN `Unknown6` `StaticInstance` int(32) NOT NULL DEFAULT 0');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mobspawnsweapons' AND COLUMN_NAME = 'Unknown7') = 0,
  'SELECT 1',
  'ALTER TABLE mobspawnsweapons CHANGE COLUMN `Unknown7` `MultipleCount` int(32) NOT NULL DEFAULT 0');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'questwire' AND COLUMN_NAME = 'Unknown21') = 0,
  'SELECT 1',
  'ALTER TABLE questwire CHANGE COLUMN `Unknown21` `RequiredCount` int(32) NOT NULL DEFAULT 0');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'questwirerewards' AND COLUMN_NAME = 'Unknown1') = 0,
  'SELECT 1',
  'ALTER TABLE questwirerewards CHANGE COLUMN `Unknown1` `Unused` int(32) NOT NULL DEFAULT 0');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

