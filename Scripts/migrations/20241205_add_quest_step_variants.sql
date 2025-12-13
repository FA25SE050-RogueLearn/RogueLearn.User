-- Script: Migration (Quest Step Variants)
-- Summary: Add difficulty_variant and module_number to quest_steps
-- Migration: Add difficulty_variant and module_number to quest_steps
-- Purpose: Support "Parallel Steps" architecture. Each logical module (e.g., Week 1) 
-- will have multiple step records, one for each difficulty variant.

-- Add difficulty_variant column
-- Values: 'Supportive', 'Standard', 'Challenging'
ALTER TABLE quest_steps
ADD COLUMN IF NOT EXISTS difficulty_variant VARCHAR(50) DEFAULT 'Standard';

-- Add module_number column
-- This groups the variants together. E.g., Module 1 will have 3 steps with module_number = 1
-- but different difficulty_variants.
ALTER TABLE quest_steps
ADD COLUMN IF NOT EXISTS module_number INTEGER DEFAULT 1;

-- Add comment for documentation
COMMENT ON COLUMN quest_steps.difficulty_variant IS 'The difficulty tier of this specific step content (Supportive, Standard, Challenging).';
COMMENT ON COLUMN quest_steps.module_number IS 'The logical sequence number of the module this step belongs to. Used to group variants.';

-- Create composite index for efficient lookup by quest + module + difficulty
CREATE INDEX IF NOT EXISTS idx_quest_steps_lookup ON quest_steps(quest_id, module_number, difficulty_variant);