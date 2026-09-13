-- WeaponItemFullUpdate has two placement bytes and optional timing/energy
-- stats. Preserve those converted capture fields instead of forcing every
-- weapon into the right hand with one seven-stat packet shape.

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mobspawnsweapons'
     AND COLUMN_NAME = 'InventoryId') = 0,
  'ALTER TABLE mobspawnsweapons ADD COLUMN InventoryId int(32) DEFAULT NULL AFTER WeaponInstance',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mobspawnsweapons'
     AND COLUMN_NAME = 'BodyLocation') = 0,
  'ALTER TABLE mobspawnsweapons ADD COLUMN BodyLocation int(32) DEFAULT NULL AFTER InventoryId',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mobspawnsweapons'
     AND COLUMN_NAME = 'ItemDelay') = 0,
  'ALTER TABLE mobspawnsweapons ADD COLUMN ItemDelay int(32) DEFAULT NULL AFTER Unknown7',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mobspawnsweapons'
     AND COLUMN_NAME = 'RechargeDelay') = 0,
  'ALTER TABLE mobspawnsweapons ADD COLUMN RechargeDelay int(32) DEFAULT NULL AFTER ItemDelay',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;

SET @omnicell_sql = IF(
  (SELECT COUNT(*) FROM information_schema.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mobspawnsweapons'
     AND COLUMN_NAME = 'Energy') = 0,
  'ALTER TABLE mobspawnsweapons ADD COLUMN Energy int(32) DEFAULT NULL AFTER RechargeDelay',
  'SELECT 1');
PREPARE omnicell_stmt FROM @omnicell_sql;
EXECUTE omnicell_stmt;
DEALLOCATE PREPARE omnicell_stmt;
