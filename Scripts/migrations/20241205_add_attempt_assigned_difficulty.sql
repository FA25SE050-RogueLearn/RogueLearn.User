-- Migration: Add assigned_difficulty to user_quest_attempts
-- Purpose: Support the "Master Quest" architecture where personalization happens at runtime via filtering.
-- The difficulty is now a property of the *attempt* (User Context), not the *quest definition* (Shared Content).

-- Add assigned_difficulty column
-- Values align with our logic: 'Challenging', 'Standard', 'Supportive', 'Adaptive'
ALTER TABLE user_quest_attempts
ADD COLUMN IF NOT EXISTS assigned_difficulty VARCHAR(50) DEFAULT 'Standard';

-- Add comment for documentation
COMMENT ON COLUMN user_quest_attempts.assigned_difficulty IS 'The personalization level assigned to this user for this specific quest attempt (Standard, Supportive, Challenging, Adaptive). Used to filter Master Quest content.';

-- Create index for reporting/analytics
CREATE INDEX IF NOT EXISTS idx_user_quest_attempts_difficulty ON user_quest_attempts(assigned_difficulty);