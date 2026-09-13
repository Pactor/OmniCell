-- Adds explicit answer-triggered quest actions to databases created before
-- this column became part of knubotscript. Safe to run on a fresh database.
SET @has_quest_dialogue_action = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'knubotscript'
    AND COLUMN_NAME = 'Action'
);
SET @add_quest_dialogue_action = IF(
  @has_quest_dialogue_action = 0,
  'ALTER TABLE knubotscript ADD COLUMN Action int(32) NOT NULL DEFAULT 0 AFTER Grants',
  'SELECT 1'
);
PREPARE quest_dialogue_action_statement FROM @add_quest_dialogue_action;
EXECUTE quest_dialogue_action_statement;
DEALLOCATE PREPARE quest_dialogue_action_statement;
