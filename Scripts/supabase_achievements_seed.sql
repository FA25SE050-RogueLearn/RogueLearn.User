-- Supabase/PostgreSQL Seed Script: Achievements catalog
-- Story 1.6: Skills and Achievements Management
-- This script ensures the achievements table supports Story 1.6 fields and upserts a predefined catalog by unique key.

-- 1) Ensure schema columns exist (non-destructive). Some columns are additions beyond the base schema.
DO $$
BEGIN
  -- Add new columns if they don't exist yet
  ALTER TABLE achievements ADD COLUMN IF NOT EXISTS key TEXT;
  ALTER TABLE achievements ADD COLUMN IF NOT EXISTS rule_type TEXT;       -- e.g., threshold | streak | composite
  ALTER TABLE achievements ADD COLUMN IF NOT EXISTS rule_config JSONB;    -- JSON rule configuration
  ALTER TABLE achievements ADD COLUMN IF NOT EXISTS category TEXT;        -- e.g., core | progression | quests | codebattle | study | skills
  ALTER TABLE achievements ADD COLUMN IF NOT EXISTS icon TEXT;            -- icon name (logical), separate from optional icon_url
  ALTER TABLE achievements ADD COLUMN IF NOT EXISTS version INTEGER DEFAULT 1;
  ALTER TABLE achievements ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;

  -- Create a unique index on key for idempotent upserts
  CREATE UNIQUE INDEX IF NOT EXISTS achievements_key_uindex ON achievements (key);
EXCEPTION WHEN OTHERS THEN
  RAISE NOTICE 'Achievements column/index migration step: %', SQLERRM;
END $$;

-- 2) Upsert predefined achievements catalog (idempotent by key)
INSERT INTO achievements (key, name, description, rule_type, rule_config, category, icon, version, is_active, source_service)
VALUES
  ('first_quest_completed','First Quest Completed','Complete your first quest','threshold','{"questsCompleted":1}','core','trophy',1,TRUE,'UserService'),
  ('xp_100','100 XP Earned','Reach a total of 100 XP','threshold','{"totalXp":100}','progression','star',1,TRUE,'UserService'),
  ('xp_500','500 XP Earned','Reach a total of 500 XP','threshold','{"totalXp":500}','progression','star',1,TRUE,'UserService'),
  ('xp_1000','1000 XP Earned','Reach a total of 1000 XP','threshold','{"totalXp":1000}','progression','star',1,TRUE,'UserService'),
  ('quests_5_completed','Quest Apprentice','Complete 5 quests','threshold','{"questsCompleted":5}','quests','trophy',1,TRUE,'UserService'),
  ('quests_20_completed','Quest Journeyman','Complete 20 quests','threshold','{"questsCompleted":20}','quests','trophy',1,TRUE,'UserService'),
  ('code_battle_win_1','Code Battle Winner','Win your first code battle','threshold','{"codeBattlesWon":1}','codebattle','medal',1,TRUE,'UserService'),
  ('code_battle_win_10','Code Battle Champion','Win 10 code battles','threshold','{"codeBattlesWon":10}','codebattle','medal',1,TRUE,'UserService'),
  ('study_streak_7','Study Streak 7','Study 7 consecutive days','streak','{"days":7}','study','flame',1,TRUE,'UserService'),
  ('skill_level_1_any','Skill Novice','Reach level 1 in any skill','threshold','{"skillLevelAny":1}','skills','badge',1,TRUE,'UserService'),
  ('skill_level_5_any','Skill Apprentice','Reach level 5 in any skill','threshold','{"skillLevelAny":5}','skills','badge',1,TRUE,'UserService'),
  ('skill_level_10_any','Skill Master','Reach level 10 in any skill','threshold','{"skillLevelAny":10}','skills','badge',1,TRUE,'UserService')
ON CONFLICT (key) DO UPDATE SET
  name         = EXCLUDED.name,
  description  = EXCLUDED.description,
  rule_type    = EXCLUDED.rule_type,
  rule_config  = EXCLUDED.rule_config,
  category     = EXCLUDED.category,
  icon         = EXCLUDED.icon,
  version      = EXCLUDED.version,
  is_active    = EXCLUDED.is_active,
  source_service = EXCLUDED.source_service;

-- Notes:
-- - Upsert is idempotent thanks to the unique index on achievements.key.
-- - icon is a logical name (e.g., 'trophy'); icon_url can be managed via Supabase storage policies (see supabase_achievements_storage_policies.sql).
-- - source_service is set to 'UserService' here; adjust if specific services own particular achievements.
-- - This seed aligns with Acceptance Criteria and Dev Notes in 1.6.skills-and-achievements-management.md.