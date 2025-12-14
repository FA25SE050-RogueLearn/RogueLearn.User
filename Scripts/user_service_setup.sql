-- Script: Full Supabase Setup
-- Summary: Creates enums, tables, indexes, functions, storage policies, and seeds

-- Script: Database Enums
-- Summary: Enum types used across RogueLearn database schema
-- User management enums
CREATE TYPE verification_status AS ENUM ('Pending', 'Approved', 'Rejected');
CREATE TYPE enrollment_status AS ENUM ('Active', 'Inactive', 'Graduated', 'Dropped', 'Suspended');
CREATE TYPE skill_relationship_type AS ENUM ('Prerequisite', 'Corequisite', 'Recommended');
CREATE TYPE skill_reward_source_type AS ENUM ('QuestComplete', 'BossFight', 'PartyActivity', 'GuildActivity', 'MeetingParticipation', 'CodeChallenge');
CREATE TYPE notification_type AS ENUM ('Achievement', 'QuestComplete', 'PartyInvite', 'GuildInvite', 'FriendRequest', 'System', 'Reminder');
CREATE TYPE skill_tier_level AS ENUM ('Foundation', 'Intermediate', 'Advanced', 'Expert');
CREATE TYPE degree_level AS ENUM ('Associate', 'Bachelor', 'Master', 'Doctorate');
CREATE TYPE subject_enrollment_status AS ENUM ('Enrolled', 'Completed', 'Failed', 'Withdrawn');

-- Quest and learning path enums
CREATE TYPE quest_type AS ENUM ('Tutorial', 'Practice', 'Challenge', 'Project', 'Assessment', 'Exploration');
CREATE TYPE difficulty_level AS ENUM ('Beginner', 'Intermediate', 'Advanced', 'Expert');
CREATE TYPE step_type AS ENUM ('Reading', 'Video', 'Interactive', 'Coding', 'Quiz', 'Discussion', 'Submission', 'Reflection');
CREATE TYPE quest_attempt_status AS ENUM ('InProgress', 'Completed', 'Abandoned', 'Paused');
CREATE TYPE step_completion_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Skipped');
CREATE TYPE path_type AS ENUM ('Course', 'Specialization', 'Bootcamp', 'Custom');
CREATE TYPE path_progress_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Paused');
CREATE TYPE quest_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Abandoned');

-- Social interaction enums
CREATE TYPE party_type AS ENUM ('StudyGroup', 'ProjectTeam', 'PeerReview', 'Casual', 'Competition');
CREATE TYPE party_role AS ENUM ('Leader', 'Member');
CREATE TYPE guild_role AS ENUM ('GuildMaster', 'Officer', 'Veteran', 'Member', 'Recruit');
CREATE TYPE member_status AS ENUM ('Active', 'Inactive', 'Suspended', 'Left');
CREATE TYPE invitation_status AS ENUM ('Pending', 'Accepted', 'Declined', 'Expired', 'Cancelled');
CREATE TYPE invitation_type AS ENUM ('Invite', 'Application');
CREATE TYPE guild_type AS ENUM ('Academic', 'Professional', 'Hobby', 'Competition', 'Study', 'Research');
CREATE TYPE activity_type AS ENUM ('QuestCompletion', 'StudySession', 'ProjectWork', 'Discussion', 'Competition', 'Meeting');
CREATE TYPE post_type AS ENUM ('announcement', 'discussion', 'general', 'achievement');

-- Guild join request enums
CREATE TYPE guild_join_request_status AS ENUM ('Pending', 'Accepted', 'Declined', 'Expired', 'Cancelled');

-- Meeting management enums
CREATE TYPE transcript_segment_status AS ENUM ('Processed', 'Failed');

-- Guild post moderation/status enums
CREATE TYPE guild_post_status AS ENUM ('published', 'pending', 'rejected');
CREATE TYPE personalized_difficulty AS ENUM ('Supportive', 'Standard', 'Challenging', 'Adaptive');
CREATE TYPE subject_progress_status AS ENUM ('Passed', 'NotPassed', 'Studying', 'NotStarted');

-- Summary: Core tables for users, curriculum, quests, social, and analytics
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    permissions JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE classes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    roadmap_url TEXT,
    skill_focus_areas TEXT[],
    difficulty_level difficulty_level DEFAULT 'Beginner',
    estimated_duration_months INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE curriculum_programs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    program_name VARCHAR(255) NOT NULL,
    program_code VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    degree_level degree_level NOT NULL,
    total_credits INTEGER,
    duration_years INTEGER,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE subjects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subject_code VARCHAR(50) NOT NULL UNIQUE,
    subject_name VARCHAR(255) NOT NULL,
    credits INTEGER NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    domain VARCHAR(100),
    tier skill_tier_level NOT NULL DEFAULT 'Foundation',
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE achievements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL UNIQUE,
    description TEXT NOT NULL,
    icon_url TEXT,
    source_service VARCHAR(255) NOT NULL,
    key TEXT,
    rule_type TEXT,
    rule_config JSONB,
    category TEXT,
    version INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT true,
    merit_points_reward INTEGER,
    contribution_points_reward INTEGER,
    is_medal BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE user_profiles (
    auth_user_id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    username VARCHAR(255) NOT NULL UNIQUE,
    bio TEXT,
    email VARCHAR(255) NOT NULL UNIQUE,
    first_name VARCHAR(255),
    last_name VARCHAR(255),
    class_id UUID REFERENCES classes(id),
    route_id UUID REFERENCES curriculum_programs(id),
    level INTEGER NOT NULL DEFAULT 1,
    experience_points INTEGER NOT NULL DEFAULT 0,
    profile_image_url TEXT,
    preferences JSONB,
    onboarding_completed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    assigned_by UUID REFERENCES user_profiles(auth_user_id),
    UNIQUE (auth_user_id, role_id)
);

CREATE TABLE lecturer_verification_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    status verification_status NOT NULL DEFAULT 'Pending',
    documents JSONB,
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    reviewed_at TIMESTAMPTZ,
    reviewer_id UUID REFERENCES user_profiles(auth_user_id),
    review_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE class_specialization_subjects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    class_id UUID NOT NULL REFERENCES classes(id) ON DELETE CASCADE,
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    placeholder_subject_code TEXT NOT NULL,
    semester INTEGER NOT NULL
);

CREATE TABLE curriculum_program_subjects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    program_id UUID NOT NULL REFERENCES curriculum_programs(id) ON DELETE CASCADE,
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (program_id, subject_id)
);

CREATE TABLE notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    content JSONB,
    is_public BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    UNIQUE (auth_user_id, name)
);

CREATE TABLE note_tags (
    note_id UUID NOT NULL REFERENCES notes(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (note_id, tag_id)
);

CREATE TABLE user_skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    skill_name VARCHAR(255) NOT NULL,
    experience_points INTEGER NOT NULL DEFAULT 0,
    level INTEGER NOT NULL DEFAULT 1,
    last_updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (auth_user_id, skill_name)
);

CREATE TABLE user_skill_rewards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    source_service VARCHAR(100) NOT NULL,
    source_type skill_reward_source_type NOT NULL,
    source_id UUID,
    skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    skill_name VARCHAR(255) NOT NULL,
    points_awarded INTEGER NOT NULL,
    reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE user_achievements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    achievement_id UUID NOT NULL REFERENCES achievements(id) ON DELETE CASCADE,
    earned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    context JSONB
);

CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    type notification_type NOT NULL,
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    read_at TIMESTAMPTZ
);

CREATE TABLE subject_skill_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    relevance_weight DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (subject_id, skill_id)
);

CREATE TABLE student_enrollments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    enrollment_date DATE NOT NULL,
    expected_graduation_date DATE,
    status enrollment_status NOT NULL DEFAULT 'Active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE student_semester_subjects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    enrollment_id UUID NOT NULL REFERENCES student_enrollments(id) ON DELETE CASCADE,
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    academic_year VARCHAR(10) NOT NULL,
    semester INTEGER NOT NULL,
    status subject_enrollment_status NOT NULL DEFAULT 'Enrolled',
    grade VARCHAR(5),
    credits_earned INTEGER DEFAULT 0,
    enrolled_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    UNIQUE (enrollment_id, subject_id, academic_year, semester)
);

CREATE TABLE skill_dependencies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    prerequisite_skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    relationship_type skill_relationship_type NOT NULL DEFAULT 'Prerequisite',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (skill_id, prerequisite_skill_id, relationship_type)
);

CREATE TABLE learning_paths (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    path_type path_type NOT NULL,
    estimated_total_duration_hours INTEGER,
    total_experience_points INTEGER NOT NULL DEFAULT 0,
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE quests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    quest_type quest_type NOT NULL,
    difficulty_level difficulty_level NOT NULL,
    estimated_duration_minutes INTEGER,
    experience_points_reward INTEGER NOT NULL DEFAULT 0,
    sequence INTEGER,
    skill_tags TEXT[],
    subject_id UUID REFERENCES subjects(id),
    quest_status quest_status DEFAULT 'NotStarted',
    is_recommended BOOLEAN NOT NULL DEFAULT FALSE,
    recommendation_reason TEXT,
    expected_difficulty personalized_difficulty DEFAULT 'Standard',
    difficulty_reason TEXT,
    subject_grade VARCHAR(10),
    subject_status subject_progress_status,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_by UUID REFERENCES user_profiles(auth_user_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE quest_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quest_id UUID NOT NULL REFERENCES quests(id) ON DELETE CASCADE,
    step_number INTEGER NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    step_type step_type NOT NULL,
    difficulty_variant personalized_difficulty DEFAULT 'Standard',
    module_number INTEGER DEFAULT 1,
    content JSONB,
    validation_criteria JSONB,
    experience_points INTEGER NOT NULL DEFAULT 0,
    is_optional BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (quest_id, step_number)
);

CREATE TABLE user_quest_attempts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    quest_id UUID NOT NULL REFERENCES quests(id) ON DELETE CASCADE,
    status quest_attempt_status NOT NULL DEFAULT 'InProgress',
    assigned_difficulty personalized_difficulty DEFAULT 'Standard',
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    abandoned_at TIMESTAMPTZ,
    total_experience_earned INTEGER NOT NULL DEFAULT 0,
    completion_percentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    current_step_id UUID REFERENCES quest_steps(id),
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (auth_user_id, quest_id)
);

CREATE TABLE user_quest_step_progress (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    attempt_id UUID NOT NULL REFERENCES user_quest_attempts(id) ON DELETE CASCADE,
    step_id UUID NOT NULL REFERENCES quest_steps(id) ON DELETE CASCADE,
    status step_completion_status NOT NULL DEFAULT 'NotStarted',
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    submission_data JSONB,
    feedback TEXT,
    experience_earned INTEGER NOT NULL DEFAULT 0,
    attempts_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (attempt_id, step_id)
);

CREATE TABLE user_quest_step_feedback (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quest_id UUID NOT NULL REFERENCES quests(id) ON DELETE CASCADE,
    step_id UUID NOT NULL REFERENCES quest_steps(id) ON DELETE CASCADE,
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    rating INTEGER NOT NULL,
    category TEXT NOT NULL,
    comment TEXT,
    admin_notes TEXT,
    is_resolved BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE parties (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    party_type party_type NOT NULL,
    max_members INTEGER NOT NULL DEFAULT 6,
    current_member_count INTEGER NOT NULL DEFAULT 1,
    is_public BOOLEAN NOT NULL DEFAULT TRUE,
    created_by UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    disbanded_at TIMESTAMPTZ
);

CREATE TABLE party_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    party_id UUID NOT NULL REFERENCES parties(id) ON DELETE CASCADE,
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    role party_role NOT NULL DEFAULT 'Member',
    status member_status NOT NULL DEFAULT 'Active',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    left_at TIMESTAMPTZ,
    contribution_score INTEGER NOT NULL DEFAULT 0,
    UNIQUE (party_id, auth_user_id)
);

CREATE TABLE party_invitations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    party_id UUID NOT NULL REFERENCES parties(id) ON DELETE CASCADE,
    inviter_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    invitee_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    status invitation_status NOT NULL DEFAULT 'Pending',
    message TEXT,
    invited_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    responded_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ NOT NULL DEFAULT (now() + INTERVAL '7 days'),
    UNIQUE (party_id, invitee_id)
);

CREATE TABLE guilds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    description TEXT NOT NULL,
    guild_type guild_type NOT NULL,
    max_members INTEGER NOT NULL DEFAULT 100,
    current_member_count INTEGER NOT NULL DEFAULT 1,
    merit_points INTEGER NOT NULL DEFAULT 0,
    is_public BOOLEAN NOT NULL DEFAULT TRUE,
    is_lecturer_guild BOOLEAN NOT NULL DEFAULT FALSE,
    requires_approval BOOLEAN NOT NULL DEFAULT FALSE,
    banner_image_url TEXT,
    created_by UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE guild_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id UUID NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    role guild_role NOT NULL DEFAULT 'Member',
    status member_status NOT NULL DEFAULT 'Active',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    left_at TIMESTAMPTZ,
    contribution_points INTEGER NOT NULL DEFAULT 0,
    rank_within_guild INTEGER,
    UNIQUE (guild_id, auth_user_id)
);

CREATE TABLE guild_invitations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id UUID NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
    inviter_id UUID REFERENCES user_profiles(auth_user_id),
    invitee_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    invitation_type invitation_type NOT NULL,
    status invitation_status NOT NULL DEFAULT 'Pending',
    message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    responded_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ NOT NULL DEFAULT (now() + INTERVAL '14 days'),
    UNIQUE (guild_id, invitee_id)
);

CREATE TABLE guild_join_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id UUID NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
    requester_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    status guild_join_request_status NOT NULL DEFAULT 'Pending',
    message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    responded_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ NOT NULL DEFAULT (now() + INTERVAL '14 days'),
    UNIQUE (guild_id, requester_id)
);

CREATE TABLE guild_posts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id UUID NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
    author_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    post_type post_type NOT NULL DEFAULT 'general',
    is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
    status guild_post_status NOT NULL DEFAULT 'published',
    is_locked BOOLEAN NOT NULL DEFAULT FALSE,
    is_announcement BOOLEAN NOT NULL DEFAULT FALSE,
    attachments JSONB,
    tags TEXT[],
    like_count INTEGER NOT NULL DEFAULT 0,
    comment_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE guild_post_comments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    post_id UUID NOT NULL REFERENCES guild_posts(id) ON DELETE CASCADE,
    author_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    content TEXT NOT NULL,
    parent_comment_id UUID REFERENCES guild_post_comments(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE guild_post_likes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    post_id UUID NOT NULL REFERENCES guild_posts(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (post_id, user_id)
);

CREATE TABLE meetings (
    meeting_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    party_id UUID REFERENCES parties(id) ON DELETE CASCADE,
    guild_id UUID REFERENCES guilds(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    scheduled_start_time TIMESTAMPTZ NOT NULL,
    scheduled_end_time TIMESTAMPTZ NOT NULL,
    actual_start_time TIMESTAMPTZ,
    actual_end_time TIMESTAMPTZ,
    organizer_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    meeting_link TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE meeting_participants (
    participant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID NOT NULL REFERENCES meetings(meeting_id) ON DELETE CASCADE,
    user_id UUID REFERENCES user_profiles(auth_user_id),
    display_name TEXT,
    join_time TIMESTAMPTZ,
    leave_time TIMESTAMPTZ,
    role_in_meeting VARCHAR(50) NOT NULL DEFAULT 'participant'
);

CREATE TABLE meeting_summaries (
    meeting_summary_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID NOT NULL REFERENCES meetings(meeting_id) ON DELETE CASCADE,
    summary_text TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE party_stash_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    party_id UUID NOT NULL REFERENCES parties(id) ON DELETE CASCADE,
    original_note_id UUID NOT NULL REFERENCES notes(id),
    shared_by_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    title TEXT NOT NULL,
    content JSONB NOT NULL,
    tags TEXT[],
    shared_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE quest_submissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id),
    quest_id UUID NOT NULL REFERENCES quests(id) ON DELETE CASCADE,
    step_id UUID REFERENCES quest_steps(id) ON DELETE SET NULL,
    activity_id UUID,
    attempt_id UUID NOT NULL REFERENCES user_quest_attempts(id) ON DELETE CASCADE,
    submission_data TEXT NOT NULL,
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    graded_at TIMESTAMPTZ,
    grade DECIMAL(5,2),
    max_grade DECIMAL(5,2) NOT NULL,
    feedback TEXT,
    is_passed BOOLEAN,
    attempt_number INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE match_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    match_id TEXT NOT NULL,
    start_utc TIMESTAMPTZ NOT NULL,
    end_utc TIMESTAMPTZ NOT NULL,
    result TEXT NOT NULL,
    scene TEXT NOT NULL,
    total_players INTEGER NOT NULL,
    user_id UUID REFERENCES user_profiles(auth_user_id),
    match_data TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE game_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id UUID NOT NULL,
    user_id UUID REFERENCES user_profiles(auth_user_id),
    relay_join_code TEXT,
    pack_id TEXT,
    subject TEXT,
    topic TEXT,
    difficulty TEXT,
    question_pack TEXT,
    status TEXT NOT NULL DEFAULT 'created',
    match_result_id UUID REFERENCES match_results(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ
);

CREATE TABLE match_player_summaries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    match_result_id UUID NOT NULL REFERENCES match_results(id) ON DELETE CASCADE,
    session_id UUID,
    user_id UUID REFERENCES user_profiles(auth_user_id),
    client_id BIGINT,
    total_questions INTEGER NOT NULL,
    correct_answers INTEGER NOT NULL,
    average_time DOUBLE PRECISION,
    topic_breakdown TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Script: Database Indexes
-- Summary: Performance indexes grouped by domain area
-- User profile indexes
CREATE INDEX idx_user_profiles_username ON user_profiles(username);
CREATE INDEX idx_user_profiles_email ON user_profiles(email);
CREATE INDEX idx_user_profiles_class_id ON user_profiles(class_id);

-- Roadmap.sh specialization indexes
CREATE INDEX idx_classes_name ON classes(name);
CREATE INDEX idx_classes_is_active ON classes(is_active);
CREATE INDEX idx_classes_difficulty_level ON classes(difficulty_level);
-- Class mapping indexes
CREATE INDEX idx_class_specialization_subjects_class_id ON class_specialization_subjects(class_id);
CREATE INDEX idx_class_specialization_subjects_subject_id ON class_specialization_subjects(subject_id);

CREATE INDEX idx_student_enrollments_auth_user_id ON student_enrollments(auth_user_id);
CREATE INDEX idx_student_semester_subjects_enrollment_id ON student_semester_subjects(enrollment_id);

-- Notes/Arsenal indexes
CREATE INDEX idx_notes_auth_user_id ON notes(auth_user_id);
 
CREATE INDEX idx_tags_auth_user_id ON tags(auth_user_id);
CREATE INDEX idx_note_tags_note_id ON note_tags(note_id);
CREATE INDEX idx_note_tags_tag_id ON note_tags(tag_id);

CREATE INDEX idx_user_skills_auth_user_id ON user_skills(auth_user_id);
CREATE INDEX idx_user_quest_attempts_difficulty ON user_quest_attempts(assigned_difficulty);
CREATE INDEX idx_skills_name ON skills(name);
CREATE INDEX idx_skills_domain ON skills(domain);
CREATE INDEX idx_skill_dependencies_skill_id ON skill_dependencies(skill_id);
CREATE INDEX idx_skill_dependencies_prereq_id ON skill_dependencies(prerequisite_skill_id);
CREATE INDEX idx_user_skill_rewards_auth_user_id ON user_skill_rewards(auth_user_id);
CREATE INDEX idx_user_skill_rewards_skill_name ON user_skill_rewards(skill_name);
CREATE INDEX idx_user_skill_rewards_source_service ON user_skill_rewards(source_service);
CREATE INDEX idx_user_skill_rewards_created_at ON user_skill_rewards(created_at);

-- Achievement indexes
CREATE INDEX idx_user_achievements_auth_user_id ON user_achievements(auth_user_id);
CREATE INDEX idx_user_achievements_achievement_id ON user_achievements(achievement_id);

CREATE UNIQUE INDEX IF NOT EXISTS achievements_key_uindex ON achievements (key);
CREATE INDEX idx_quests_expected_difficulty ON quests(expected_difficulty);
CREATE INDEX idx_quests_subject_status ON quests(subject_status);
CREATE INDEX idx_quest_steps_lookup ON quest_steps(quest_id, module_number, difficulty_variant);

-- Game and match indexes
CREATE INDEX idx_match_results_user_id ON match_results(user_id);
CREATE INDEX idx_game_sessions_user_id ON game_sessions(user_id);
CREATE INDEX idx_match_player_summaries_result_id ON match_player_summaries(match_result_id);

-- Subject skill mapping and curriculum mapping indexes
CREATE INDEX idx_subject_skill_mappings_subject_id ON subject_skill_mappings(subject_id);
CREATE INDEX idx_subject_skill_mappings_skill_id ON subject_skill_mappings(skill_id);
CREATE INDEX idx_curriculum_program_subjects_program_id ON curriculum_program_subjects(program_id);
CREATE INDEX idx_curriculum_program_subjects_subject_id ON curriculum_program_subjects(subject_id);

-- Notification indexes
CREATE INDEX idx_notifications_auth_user_id ON notifications(auth_user_id);
CREATE INDEX idx_notifications_type ON notifications(type);
CREATE INDEX idx_notifications_is_read ON notifications(is_read);
CREATE INDEX idx_notifications_created_at ON notifications(created_at);


-- Script: Custom Access Token Hook
-- Summary: Adds user's roles to JWT claims for RBAC
CREATE OR REPLACE FUNCTION public.custom_access_token_hook(event jsonb)
RETURNS jsonb
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = ''
AS $$
DECLARE
    claims jsonb;
    user_roles text[];
BEGIN
    claims := event->'claims';

    SELECT array_agg(r.name)
    INTO user_roles
    FROM public.user_roles ur
    JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.auth_user_id = (event->>'user_id')::uuid;

    IF user_roles IS NOT NULL AND array_length(user_roles, 1) > 0 THEN
        claims := jsonb_set(claims, '{roles}', to_jsonb(user_roles));
    ELSE
        claims := jsonb_set(claims, '{roles}', '[]'::jsonb);
    END IF;

    event := jsonb_set(event, '{claims}', claims);
    RETURN event;
END;
$$;

GRANT USAGE ON SCHEMA public TO supabase_auth_admin;
GRANT EXECUTE ON FUNCTION public.custom_access_token_hook TO supabase_auth_admin;
REVOKE EXECUTE ON FUNCTION public.custom_access_token_hook FROM authenticated, anon, public;
GRANT ALL ON TABLE public.user_roles TO supabase_auth_admin;
GRANT ALL ON TABLE public.roles TO supabase_auth_admin;

CREATE POLICY "Allow auth admin to read user roles" ON public.user_roles
AS PERMISSIVE FOR SELECT
TO supabase_auth_admin
USING (true);

CREATE POLICY "Allow auth admin to read roles" ON public.roles
AS PERMISSIVE FOR SELECT
TO supabase_auth_admin
USING (true);

CREATE OR REPLACE FUNCTION public.authorize_role(required_role text)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = ''
AS $$
DECLARE
    user_roles text[];
BEGIN
    SELECT array(SELECT jsonb_array_elements_text(auth.jwt() -> 'roles'))
    INTO user_roles;
    RETURN required_role = ANY(user_roles);
END;
$$;

-- Script: Get Full User Info
-- Summary: Returns a JSON bundle of profile, relations, and counts
create or replace function public.get_full_user_info(
  p_auth_user_id uuid,
  p_page_size int,
  p_page_number int
)
returns jsonb
language sql
security definer
set search_path = public
as $$
with profile as (
  select up.*
  from user_profiles up
  where up.auth_user_id = p_auth_user_id
),
class_name as (
  select c.name
  from classes c
  join profile p on c.id = p.class_id
),
curriculum_name as (
  select cp.program_name
  from curriculum_programs cp
  join profile p on cp.id = p.route_id
),
roles as (
  select jsonb_agg(jsonb_build_object(
    'roleId', ur.role_id,
    'assignedAt', ur.assigned_at,
    'roleName', r.name
  )) as data
  from user_roles ur
  left join roles r on r.id = ur.role_id
  where ur.auth_user_id = p_auth_user_id
),
enrollments as (
  select jsonb_agg(jsonb_build_object(
    'id', se.id,
    'status', se.status,
    'enrollmentDate', se.enrollment_date,
    'expectedGraduationDate', se.expected_graduation_date
  )) as data
  from student_enrollments se
  where se.auth_user_id = p_auth_user_id
),
subjects as (
  select jsonb_agg(jsonb_build_object(
    'id', sts.id,
    'subjectId', sts.subject_id,
    'subjectCode', s.subject_code,
    'subjectName', s.subject_name,
    'semester', sts.semester,
    'status', sts.status,
    'grade', sts.grade
  ) order by sts.semester nulls last) as data
  from student_semester_subjects sts
  join student_enrollments se on se.id = sts.enrollment_id
  left join subjects s on s.id = sts.subject_id
  where se.auth_user_id = p_auth_user_id
),
skills as (
  select jsonb_agg(jsonb_build_object(
    'id', us.id,
    'skillName', us.skill_name,
    'level', us.level,
    'experiencePoints', us.experience_points
  )) as data
  from user_skills us
  where us.auth_user_id = p_auth_user_id
),
achievements as (
  select jsonb_agg(jsonb_build_object(
    'achievementId', ua.achievement_id,
    'earnedAt', ua.earned_at,
    'achievementName', a.name,
    'achievementIconUrl', a.icon_url
  )) as data
  from user_achievements ua
  left join achievements a on a.id = ua.achievement_id
  where ua.auth_user_id = p_auth_user_id
),
party_members as (
  select jsonb_agg(jsonb_build_object(
    'partyId', pm.party_id,
    'partyName', p.name,
    'role', pm.role,
    'joinedAt', pm.joined_at
  )) as data
  from party_members pm
  left join parties p on p.id = pm.party_id
  where pm.auth_user_id = p_auth_user_id
),
guild_members as (
  select jsonb_agg(jsonb_build_object(
    'guildId', gm.guild_id,
    'guildName', g.name,
    'role', gm.role,
    'joinedAt', gm.joined_at
  )) as data
  from guild_members gm
  left join guilds g on g.id = gm.guild_id
  where gm.auth_user_id = p_auth_user_id
),
notes as (
  select jsonb_agg(jsonb_build_object(
    'id', n.id,
    'title', n.title,
    'createdAt', n.created_at
  )) as data
  from (
    select *
    from notes
    where auth_user_id = p_auth_user_id
    order by created_at desc
    limit p_page_size offset (p_page_number - 1) * p_page_size
  ) n
),
notifications as (
  select jsonb_agg(jsonb_build_object(
    'id', notif.id,
    'type', notif.type,
    'title', notif.title,
    'isRead', notif.is_read,
    'createdAt', notif.created_at
  )) as data
  from (
    select *
    from notifications notif
    where notif.auth_user_id = p_auth_user_id
    order by notif.created_at desc
    limit p_page_size
  ) notif
),
lecturer_verifs as (
  select jsonb_agg(jsonb_build_object(
    'id', lvr.id,
    'status', lvr.status,
    'submittedAt', lvr.submitted_at
  )) as data
  from lecturer_verification_requests lvr
  where lvr.auth_user_id = p_auth_user_id
),
quest_attempts as (
  select jsonb_agg(jsonb_build_object(
    'attemptId', qa.id,
    'questId', qa.quest_id,
    'questTitle', q.title,
    'status', qa.status,
    'completionPercentage', qa.completion_percentage,
    'totalExperienceEarned', qa.total_experience_earned,
    'startedAt', qa.started_at,
    'completedAt', qa.completed_at,
    'stepsTotal', (
      select count(*) from quest_steps qs where qs.quest_id = qa.quest_id
    ),
    'stepsCompleted', (
      select count(*) from user_quest_step_progress usp where usp.attempt_id = qa.id and usp.status = 'Completed'
    ),
    'currentStepId', qa.current_step_id
  )) as data
  from user_quest_attempts qa
  left join quests q on q.id = qa.quest_id
  where qa.auth_user_id = p_auth_user_id
)
select jsonb_build_object(
  'profile', jsonb_build_object(
    'authUserId', (select auth_user_id from profile),
    'username', (select username from profile),
    'email', (select email from profile),
    'firstName', (select first_name from profile),
    'lastName', (select last_name from profile),
    'classId', (select class_id from profile),
    'className', (select name from class_name),
    'routeId', (select route_id from profile),
    'curriculumName', (select program_name from curriculum_name),
    'level', (select level from profile),
    'experiencePoints', (select experience_points from profile),
    'profileImageUrl', (select profile_image_url from profile),
    'onboardingCompleted', (select onboarding_completed from profile),
    'createdAt', (select created_at from profile),
    'updatedAt', (select updated_at from profile)
  ),
  'auth', jsonb_build_object(
    'id', (select auth_user_id from profile),
    'email', (select email from profile)
  ),
  'relations', jsonb_build_object(
    'userRoles', coalesce((select data from roles), '[]'::jsonb),
    'studentEnrollments', coalesce((select data from enrollments), '[]'::jsonb),
    'studentTermSubjects', coalesce((select data from subjects), '[]'::jsonb),
    'userSkills', coalesce((select data from skills), '[]'::jsonb),
    'userAchievements', coalesce((select data from achievements), '[]'::jsonb),
    'partyMembers', coalesce((select data from party_members), '[]'::jsonb),
    'guildMembers', coalesce((select data from guild_members), '[]'::jsonb),
    'notes', coalesce((select data from notes), '[]'::jsonb),
    'notifications', coalesce((select data from notifications), '[]'::jsonb),
    'lecturerVerificationRequests', coalesce((select data from lecturer_verifs), '[]'::jsonb),
    'questAttempts', coalesce((select data from quest_attempts), '[]'::jsonb)
  ),
  'counts', jsonb_build_object(
    'notes', (select count(*) from public.notes n where n.auth_user_id = p_auth_user_id),
    'achievements', (select count(*) from public.user_achievements ua where ua.auth_user_id = p_auth_user_id),
    'meetings', (select count(*) from public.meeting_participants mp where mp.user_id = p_auth_user_id),
    'notificationsUnread', (select count(*) from public.notifications notif where notif.auth_user_id = p_auth_user_id and notif.is_read = false),
    'questsCompleted', (select count(*) from public.user_quest_attempts a where a.auth_user_id = p_auth_user_id and a.status = 'Completed'),
    'questsInProgress', (select count(*) from public.user_quest_attempts a where a.auth_user_id = p_auth_user_id and a.status = 'InProgress')
  )
);
$$;

-- Script: Handle New User
-- Summary: Creates profile and assigns Player role after auth user signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
DECLARE
    student_role_id UUID;
    base_username TEXT;
    candidate_username TEXT;
    suffix INTEGER := 0;
BEGIN
    base_username := COALESCE(
        NULLIF(NEW.raw_user_meta_data->>'username', ''),
        NULLIF(split_part(NEW.email, '@', 1), ''),
        'user_' || substr(NEW.id::text, 1, 8)
    );

    candidate_username := base_username;

    WHILE EXISTS (SELECT 1 FROM public.user_profiles WHERE username = candidate_username) LOOP
        suffix := suffix + 1;
        candidate_username := base_username || '_' || suffix;
    END LOOP;

    INSERT INTO public.user_profiles (auth_user_id, email, username, first_name, last_name)
    VALUES (
        NEW.id,
        NEW.email,
        candidate_username,
        NEW.raw_user_meta_data->>'first_name',
        NEW.raw_user_meta_data->>'last_name'
    );

    SELECT id INTO student_role_id
    FROM public.roles
    WHERE name = 'Player'
    LIMIT 1;

    IF student_role_id IS NOT NULL THEN
        INSERT INTO public.user_roles (id, auth_user_id, role_id, assigned_at, assigned_by)
        VALUES (
            gen_random_uuid(),
            NEW.id,
            student_role_id,
            now(),
            NULL
        );
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

GRANT EXECUTE ON FUNCTION public.handle_new_user() TO supabase_auth_admin;
GRANT EXECUTE ON FUNCTION public.handle_new_user() TO authenticated;
GRANT EXECUTE ON FUNCTION public.handle_new_user() TO anon;

-- Script: Role Access Helpers
-- Summary: Lightweight helpers to read roles from JWT and user tables
CREATE OR REPLACE FUNCTION public.jwt_has_role(role_name text) RETURNS boolean AS $$
  SELECT COALESCE((auth.jwt() -> 'roles') ? role_name, false);
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.user_has_role(role_name text) RETURNS boolean AS $$
  SELECT EXISTS (
    SELECT 1 FROM user_roles ur
    JOIN roles r ON ur.role_id = r.id
    WHERE ur.auth_user_id = auth.uid()
    AND r.name = role_name
  );
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_game_master() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Game Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_admin() RETURNS boolean AS $$
  SELECT public.is_game_master();
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_lecturer() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Verified Lecturer');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_guild_master() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Guild Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_party_leader() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Party Leader');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_player() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Player');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_student() RETURNS boolean AS $$
  SELECT public.is_player();
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_leader() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Party Leader') OR public.jwt_has_role('Guild Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.has_elevated_access() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Verified Lecturer') OR public.jwt_has_role('Guild Master') OR public.jwt_has_role('Game Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

INSERT INTO storage.buckets (id, name, public)
VALUES ('achievements', 'achievements', true)
ON CONFLICT (id) DO UPDATE SET public = excluded.public;

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'curriculum-imports',
    'curriculum-imports',
    false,
    10485760,
    ARRAY['application/json', 'text/plain', 'text/json']
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public)
VALUES ('guild-posts', 'guild-posts', true)
ON CONFLICT (id) DO UPDATE SET public = excluded.public;

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'lecturer-verification',
    'lecturer-verification',
    true,
    5242880,
    ARRAY['image/png','image/jpeg','image/webp','image/gif','application/pdf']
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'notes-media',
  'notes-media',
  true,
  10485760,
  null
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'roadmap-imports',
    'roadmap-imports',
    false,
    20971520,
    ARRAY['application/json', 'text/plain', 'application/pdf']
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'user-avatars',
    'user-avatars',
    true,
    5242880,
    ARRAY['image/png','image/jpeg','image/webp','image/gif']
)
ON CONFLICT (id) DO NOTHING;
