CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE classes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL, -- e.g., "Backend Developer", "Frontend Developer"
    description TEXT,
    roadmap_url TEXT, -- Link to roadmap.sh specialization
    skill_focus_areas TEXT[], -- Array of primary skill domains
    difficulty_level INTEGER DEFAULT 1, -- 1=Beginner, 2=Intermediate, 3=Advanced
    estimated_duration_months INTEGER, -- Expected time to complete roadmap
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

CREATE TABLE user_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL UNIQUE REFERENCES auth.users(id) ON DELETE CASCADE,
    username VARCHAR(255) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    first_name VARCHAR(255),
    last_name VARCHAR(255),
    class_id UUID REFERENCES classes(id), -- Selected roadmap.sh specialization (Step 2)
    route_id UUID REFERENCES curriculum_programs(id), -- Selected curriculum (Step 1)
    level INTEGER NOT NULL DEFAULT 1,
    experience_points INTEGER NOT NULL DEFAULT 0,
    profile_image_url TEXT,
    onboarding_completed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (auth_user_id, role_id)
);

CREATE TABLE lecturer_verification_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    institution_name VARCHAR(255) NOT NULL,
    department VARCHAR(255),
    verification_document_url TEXT,
    status verification_status NOT NULL DEFAULT 'Pending',
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    reviewed_at TIMESTAMPTZ,
    reviewer_id UUID REFERENCES user_profiles(auth_user_id),
    notes TEXT
);

CREATE TABLE curriculum_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    program_id UUID NOT NULL REFERENCES curriculum_programs(id) ON DELETE CASCADE,
    version_code VARCHAR(50) NOT NULL,
    effective_year INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (program_id, version_code)
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

CREATE TABLE curriculum_structure (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    curriculum_version_id UUID NOT NULL REFERENCES curriculum_versions(id) ON DELETE CASCADE,
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    term_number INTEGER NOT NULL,
    is_mandatory BOOLEAN NOT NULL DEFAULT TRUE,
    prerequisite_subject_ids UUID[],
    prerequisites_text TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (curriculum_version_id, subject_id)
);

CREATE TABLE syllabus_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    version_number INTEGER NOT NULL,
    content JSONB NOT NULL,
    effective_date DATE NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_by UUID REFERENCES user_profiles(auth_user_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (subject_id, version_number)
);

CREATE TABLE elective_sources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    url TEXT NOT NULL,
    description TEXT,
    submitted_by UUID REFERENCES user_profiles(auth_user_id),
    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    reviewed_at TIMESTAMPTZ,
    reviewer_id UUID REFERENCES user_profiles(auth_user_id),
    notes TEXT
);

CREATE TABLE elective_packs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    version VARCHAR(50) NOT NULL,
    source_type VARCHAR(20) NOT NULL, -- Set, Seed
    subject_id UUID REFERENCES subjects(id),
    curriculum_version_id UUID REFERENCES curriculum_versions(id),
    metadata JSONB,
    approved_by UUID REFERENCES user_profiles(auth_user_id),
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (version, subject_id, curriculum_version_id)
);

CREATE TABLE curriculum_import_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_type VARCHAR(20) NOT NULL, -- file, api
    program_code VARCHAR(50) NOT NULL,
    file_url TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
    error_message TEXT,
    created_by UUID REFERENCES user_profiles(auth_user_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ
);

CREATE TABLE curriculum_version_activations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    curriculum_version_id UUID NOT NULL REFERENCES curriculum_versions(id) ON DELETE CASCADE,
    effective_year INTEGER NOT NULL,
    activated_by UUID REFERENCES user_profiles(auth_user_id),
    activated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    notes TEXT,
    UNIQUE (curriculum_version_id, effective_year)
);

CREATE TABLE student_enrollments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    curriculum_version_id UUID NOT NULL REFERENCES curriculum_versions(id) ON DELETE CASCADE,
    enrollment_date DATE NOT NULL,
    expected_graduation_date DATE,
    status enrollment_status NOT NULL DEFAULT 'Active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (auth_user_id, curriculum_version_id)
);

CREATE TABLE student_term_subjects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    enrollment_id UUID NOT NULL REFERENCES student_enrollments(id) ON DELETE CASCADE,
    subject_id UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    term_number INTEGER NOT NULL,
    academic_year VARCHAR(10) NOT NULL,
    semester VARCHAR(20) NOT NULL,
    status subject_enrollment_status NOT NULL DEFAULT 'Enrolled',
    grade VARCHAR(5),
    credits_earned INTEGER DEFAULT 0,
    enrolled_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    UNIQUE (enrollment_id, subject_id, term_number, academic_year, semester)
);

CREATE TABLE skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    domain VARCHAR(100), -- e.g., Programming, Mathematics, Design
    tier INTEGER NOT NULL DEFAULT 1, -- 1=Foundation, 2=Intermediate, 3=Advanced, 4=Expert
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE skill_dependencies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    prerequisite_skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    relationship_type VARCHAR(30) NOT NULL DEFAULT 'Prerequisite', -- Prerequisite, Complements, Alternative
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (skill_id, prerequisite_skill_id, relationship_type)
);

CREATE TABLE user_skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    skill_name VARCHAR(255) NOT NULL,
    experience_points INTEGER NOT NULL DEFAULT 0,
    level INTEGER NOT NULL DEFAULT 1,
    last_updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (auth_user_id, skill_name)
);

CREATE TABLE user_quest_progress (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    quest_id UUID NOT NULL,
    status quest_status NOT NULL,
    completed_at TIMESTAMPTZ,
    last_updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (auth_user_id, quest_id)
);

CREATE TABLE user_skill_rewards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    source_service VARCHAR(100) NOT NULL, -- e.g., QuestsService, CodeBattleService, MeetingService
    source_type VARCHAR(50) NOT NULL, -- e.g., QuestComplete, BossFight, PartyActivity
    source_id UUID, -- Optional reference to the originating entity
    skill_name VARCHAR(255) NOT NULL, -- Name from skills catalog
    points_awarded INTEGER NOT NULL,
    reason TEXT, -- Optional human-readable context
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE achievements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL UNIQUE,
    description TEXT NOT NULL,
    icon_url TEXT,
    source_service VARCHAR(255) NOT NULL -- e.g., 'QuestsService', 'CodeBattleService'
);

CREATE TABLE user_achievements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_user_id UUID NOT NULL REFERENCES user_profiles(auth_user_id) ON DELETE CASCADE,
    achievement_id UUID NOT NULL REFERENCES achievements(id) ON DELETE CASCADE,
    earned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    context JSONB -- To store additional details, like the event or quest that triggered the achievement
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