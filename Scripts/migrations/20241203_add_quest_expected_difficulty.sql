-- Migration: Add expected_difficulty and difficulty_reason columns to quests table
-- Purpose: Store per-user quest difficulty based on their academic scores for the related subject
-- Date: 2024-12-03

-- Add expected_difficulty column
-- Values: 'Challenging' (high score >= 8.5), 'Standard' (7.0-8.5), 'Supportive' (< 7.0 or failed), 'Adaptive' (currently studying)
ALTER TABLE quests 
ADD COLUMN IF NOT EXISTS expected_difficulty VARCHAR(50) DEFAULT 'Standard';

-- Add difficulty_reason column  
-- Stores human-readable explanation, e.g., "High score (8.7) - advanced content"
ALTER TABLE quests
ADD COLUMN IF NOT EXISTS difficulty_reason TEXT;

-- Add subject_grade column to cache the user's grade for the quest's subject at time of creation/sync
-- This helps with display and debugging without needing to re-query student_semester_subjects
ALTER TABLE quests
ADD COLUMN IF NOT EXISTS subject_grade VARCHAR(10);

-- Add subject_status column to cache the user's enrollment status for the subject
-- Values: 'Passed', 'NotPassed', 'Studying', 'NotStarted'
ALTER TABLE quests
ADD COLUMN IF NOT EXISTS subject_status VARCHAR(50);

-- Create index for filtering quests by difficulty (useful for dashboard/recommendations)
CREATE INDEX IF NOT EXISTS idx_quests_expected_difficulty ON quests(expected_difficulty);

-- Create index for filtering by subject_status
CREATE INDEX IF NOT EXISTS idx_quests_subject_status ON quests(subject_status);

-- Add comment for documentation
COMMENT ON COLUMN quests.expected_difficulty IS 'Personalized difficulty based on user academic performance: Challenging (>=8.5), Standard (7.0-8.5), Supportive (<7.0/failed), Adaptive (studying)';
COMMENT ON COLUMN quests.difficulty_reason IS 'Human-readable explanation of why this difficulty was assigned';
COMMENT ON COLUMN quests.subject_grade IS 'Cached grade from student_semester_subjects at time of quest creation/sync';
COMMENT ON COLUMN quests.subject_status IS 'Cached enrollment status: Passed, NotPassed, Studying, NotStarted';

-- Optional: Backfill existing quests with default values (run manually if needed)
-- UPDATE quests SET expected_difficulty = 'Standard', difficulty_reason = 'Legacy quest - difficulty not yet calculated' WHERE expected_difficulty IS NULL;
