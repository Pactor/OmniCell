-- Exact item identities are separate from their display names. These columns
-- keep existing community objectives compatible (zero means legacy name
-- matching) while allowing capture-backed and newly GM-authored hand-ins to
-- fail closed on the wrong template or quality.

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'questobjectives'
     AND COLUMN_NAME = 'TargetLowId') = 0,
  'ALTER TABLE questobjectives ADD COLUMN TargetLowId int(32) NOT NULL DEFAULT 0 AFTER Target',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'questobjectives'
     AND COLUMN_NAME = 'TargetHighId') = 0,
  'ALTER TABLE questobjectives ADD COLUMN TargetHighId int(32) NOT NULL DEFAULT 0 AFTER TargetLowId',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'questobjectives'
     AND COLUMN_NAME = 'TargetQuality') = 0,
  'ALTER TABLE questobjectives ADD COLUMN TargetQuality int(32) NOT NULL DEFAULT 0 AFTER TargetHighId',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;
