-- =====================================================
-- RogueLearn User Service - Row Level Security Policies
-- =====================================================
-- This script creates RLS policies for all entities in the RogueLearn User service
-- to ensure proper data access control in Supabase

-- Enable RLS on all tables first
-- =====================================================

-- User-related tables
ALTER TABLE user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_skills ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_skill_rewards ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_achievements ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_quest_progress ENABLE ROW LEVEL SECURITY;
ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;

-- Academic tables
ALTER TABLE student_enrollments ENABLE ROW LEVEL SECURITY;
ALTER TABLE student_term_subjects ENABLE ROW LEVEL SECURITY;
ALTER TABLE lecturer_verification_requests ENABLE ROW LEVEL SECURITY;

-- System/Reference tables (usually public read)
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE skills ENABLE ROW LEVEL SECURITY;
ALTER TABLE skill_dependencies ENABLE ROW LEVEL SECURITY;
ALTER TABLE classes ENABLE ROW LEVEL SECURITY;
ALTER TABLE achievements ENABLE ROW LEVEL SECURITY;
ALTER TABLE subjects ENABLE ROW LEVEL SECURITY;
ALTER TABLE curriculum_programs ENABLE ROW LEVEL SECURITY;
ALTER TABLE curriculum_versions ENABLE ROW LEVEL SECURITY;
ALTER TABLE curriculum_structure ENABLE ROW LEVEL SECURITY;
ALTER TABLE curriculum_version_activations ENABLE ROW LEVEL SECURITY;
ALTER TABLE curriculum_import_jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE syllabus_versions ENABLE ROW LEVEL SECURITY;
ALTER TABLE elective_packs ENABLE ROW LEVEL SECURITY;
ALTER TABLE elective_sources ENABLE ROW LEVEL SECURITY;

-- =====================================================

-- Note: Using Supabase's built-in auth.uid() function instead of creating custom auth.user_id()
-- auth.uid() returns the current authenticated user's UUID

-- Prefer checking JWT claim to avoid table access during policy evaluation
CREATE OR REPLACE FUNCTION public.jwt_has_role(role_name text) RETURNS boolean AS $$
  SELECT COALESCE((auth.jwt() -> 'roles') ? role_name, false);
$$ LANGUAGE sql STABLE SECURITY INVOKER;

-- Fallback function that checks tables when needed outside policy evaluation
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
  SELECT public.jwt_has_role('Lecturer');
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
  SELECT public.jwt_has_role('Lecturer') OR public.jwt_has_role('Guild Master') OR public.jwt_has_role('Game Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

-- RLS Policies
-- =====================================================

-- USER PROFILES
-- Players can only access their own profile, Game Masters can access all
DROP POLICY IF EXISTS "user_profiles_select_policy" ON user_profiles;
CREATE POLICY "user_profiles_select_policy" ON user_profiles
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_profiles_insert_policy" ON user_profiles;
CREATE POLICY "user_profiles_insert_policy" ON user_profiles
  FOR INSERT WITH CHECK (
    auth_user_id = auth.uid()
  );

DROP POLICY IF EXISTS "user_profiles_update_policy" ON user_profiles;
CREATE POLICY "user_profiles_update_policy" ON user_profiles
  FOR UPDATE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_profiles_delete_policy" ON user_profiles;
CREATE POLICY "user_profiles_delete_policy" ON user_profiles
  FOR DELETE USING (
    public.is_game_master()
  );

-- USER ROLES
-- Users can view their own roles, Game Masters can manage all roles
DROP POLICY IF EXISTS "user_roles_select_policy" ON user_roles;
CREATE POLICY "user_roles_select_policy" ON user_roles
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_roles_insert_policy" ON user_roles;
CREATE POLICY "user_roles_insert_policy" ON user_roles
  FOR INSERT WITH CHECK (
    public.is_game_master()
  );

DROP POLICY IF EXISTS "user_roles_update_policy" ON user_roles;
CREATE POLICY "user_roles_update_policy" ON user_roles
  FOR UPDATE USING (
    public.is_game_master()
  );

DROP POLICY IF EXISTS "user_roles_delete_policy" ON user_roles;
CREATE POLICY "user_roles_delete_policy" ON user_roles
  FOR DELETE USING (
    public.is_game_master()
  );

-- USER SKILLS
-- Players can manage their own skills, Game Masters can view all
DROP POLICY IF EXISTS "user_skills_select_policy" ON user_skills;
CREATE POLICY "user_skills_select_policy" ON user_skills
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_skills_insert_policy" ON user_skills;
CREATE POLICY "user_skills_insert_policy" ON user_skills
  FOR INSERT WITH CHECK (
    auth_user_id = auth.uid()
  );

DROP POLICY IF EXISTS "user_skills_update_policy" ON user_skills;
CREATE POLICY "user_skills_update_policy" ON user_skills
  FOR UPDATE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_skills_delete_policy" ON user_skills;
CREATE POLICY "user_skills_delete_policy" ON user_skills
  FOR DELETE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

-- USER SKILL REWARDS
-- Players can view their own rewards, system can insert rewards
DROP POLICY IF EXISTS "user_skill_rewards_select_policy" ON user_skill_rewards;
CREATE POLICY "user_skill_rewards_select_policy" ON user_skill_rewards
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_skill_rewards_insert_policy" ON user_skill_rewards;
CREATE POLICY "user_skill_rewards_insert_policy" ON user_skill_rewards
  FOR INSERT WITH CHECK (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

-- USER ACHIEVEMENTS
-- Players can view their own achievements, system can grant achievements
DROP POLICY IF EXISTS "user_achievements_select_policy" ON user_achievements;
CREATE POLICY "user_achievements_select_policy" ON user_achievements
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_achievements_insert_policy" ON user_achievements;
CREATE POLICY "user_achievements_insert_policy" ON user_achievements
  FOR INSERT WITH CHECK (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

-- USER QUEST PROGRESS
-- Players can manage their own quest progress
DROP POLICY IF EXISTS "user_quest_progress_select_policy" ON user_quest_progress;
CREATE POLICY "user_quest_progress_select_policy" ON user_quest_progress
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "user_quest_progress_insert_policy" ON user_quest_progress;
CREATE POLICY "user_quest_progress_insert_policy" ON user_quest_progress
  FOR INSERT WITH CHECK (
    auth_user_id = auth.uid()
  );

DROP POLICY IF EXISTS "user_quest_progress_update_policy" ON user_quest_progress;
CREATE POLICY "user_quest_progress_update_policy" ON user_quest_progress
  FOR UPDATE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

-- NOTIFICATIONS
-- Players can manage their own notifications, elevated users can create notifications
DROP POLICY IF EXISTS "notifications_select_policy" ON notifications;
CREATE POLICY "notifications_select_policy" ON notifications
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "notifications_insert_policy" ON notifications;
CREATE POLICY "notifications_insert_policy" ON notifications
  FOR INSERT WITH CHECK (
    public.has_elevated_access()
  );

DROP POLICY IF EXISTS "notifications_update_policy" ON notifications;
CREATE POLICY "notifications_update_policy" ON notifications
  FOR UPDATE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "notifications_delete_policy" ON notifications;
CREATE POLICY "notifications_delete_policy" ON notifications
  FOR DELETE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

-- STUDENT ENROLLMENTS
-- Players can view their own enrollments, Guild Masters and Game Masters can view relevant enrollments
DROP POLICY IF EXISTS "student_enrollments_select_policy" ON student_enrollments;
CREATE POLICY "student_enrollments_select_policy" ON student_enrollments
  FOR SELECT USING (
    auth_user_id = auth.uid() OR 
    public.is_game_master() OR
    public.is_guild_master()
  );

DROP POLICY IF EXISTS "student_enrollments_insert_policy" ON student_enrollments;
CREATE POLICY "student_enrollments_insert_policy" ON student_enrollments
  FOR INSERT WITH CHECK (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "student_enrollments_update_policy" ON student_enrollments;
CREATE POLICY "student_enrollments_update_policy" ON student_enrollments
  FOR UPDATE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

-- STUDENT TERM SUBJECTS
-- Players can view their own subjects, Guild Masters and Game Masters can view relevant subjects
DROP POLICY IF EXISTS "student_term_subjects_select_policy" ON student_term_subjects;
CREATE POLICY "student_term_subjects_select_policy" ON student_term_subjects
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM student_enrollments se 
      WHERE se.id = enrollment_id 
      AND se.auth_user_id = auth.uid()
    ) OR public.is_game_master() OR public.is_guild_master()
  );

DROP POLICY IF EXISTS "student_term_subjects_insert_policy" ON student_term_subjects;
CREATE POLICY "student_term_subjects_insert_policy" ON student_term_subjects
  FOR INSERT WITH CHECK (
    public.is_game_master() OR 
    EXISTS (
      SELECT 1 FROM student_enrollments se 
      WHERE se.id = enrollment_id 
      AND se.auth_user_id = auth.uid()
    )
  );

DROP POLICY IF EXISTS "student_term_subjects_update_policy" ON student_term_subjects;
CREATE POLICY "student_term_subjects_update_policy" ON student_term_subjects
  FOR UPDATE USING (
    public.is_game_master() OR public.is_guild_master()
  );

-- LECTURER VERIFICATION REQUESTS
-- Lecturers can manage their own requests, Game Masters can view all
DROP POLICY IF EXISTS "lecturer_verification_requests_select_policy" ON lecturer_verification_requests;
CREATE POLICY "lecturer_verification_requests_select_policy" ON lecturer_verification_requests
  FOR SELECT USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

DROP POLICY IF EXISTS "lecturer_verification_requests_insert_policy" ON lecturer_verification_requests;
CREATE POLICY "lecturer_verification_requests_insert_policy" ON lecturer_verification_requests
  FOR INSERT WITH CHECK (
    auth_user_id = auth.uid()
  );

DROP POLICY IF EXISTS "lecturer_verification_requests_update_policy" ON lecturer_verification_requests;
CREATE POLICY "lecturer_verification_requests_update_policy" ON lecturer_verification_requests
  FOR UPDATE USING (
    auth_user_id = auth.uid() OR public.is_game_master()
  );

-- REFERENCE/SYSTEM TABLES (Public Read Access)
-- =====================================================

-- ROLES - Public read, admin write
DROP POLICY IF EXISTS "roles_select_policy" ON roles;
CREATE POLICY "roles_select_policy" ON roles FOR SELECT USING (true);

DROP POLICY IF EXISTS "roles_insert_policy" ON roles;
CREATE POLICY "roles_insert_policy" ON roles FOR INSERT WITH CHECK (public.is_admin());

DROP POLICY IF EXISTS "roles_update_policy" ON roles;
CREATE POLICY "roles_update_policy" ON roles FOR UPDATE USING (public.is_admin());

DROP POLICY IF EXISTS "roles_delete_policy" ON roles;
CREATE POLICY "roles_delete_policy" ON roles FOR DELETE USING (public.is_admin());

-- SKILLS - Public read, admin write
DROP POLICY IF EXISTS "skills_select_policy" ON skills;
CREATE POLICY "skills_select_policy" ON skills FOR SELECT USING (true);

DROP POLICY IF EXISTS "skills_insert_policy" ON skills;
CREATE POLICY "skills_insert_policy" ON skills FOR INSERT WITH CHECK (public.is_admin());

DROP POLICY IF EXISTS "skills_update_policy" ON skills;
CREATE POLICY "skills_update_policy" ON skills FOR UPDATE USING (public.is_admin());

-- SKILL DEPENDENCIES - Public read, admin write
DROP POLICY IF EXISTS "skill_dependencies_select_policy" ON skill_dependencies;
CREATE POLICY "skill_dependencies_select_policy" ON skill_dependencies FOR SELECT USING (true);

DROP POLICY IF EXISTS "skill_dependencies_insert_policy" ON skill_dependencies;
CREATE POLICY "skill_dependencies_insert_policy" ON skill_dependencies FOR INSERT WITH CHECK (public.is_admin());

-- CLASSES - Public read, admin write
DROP POLICY IF EXISTS "classes_select_policy" ON classes;
CREATE POLICY "classes_select_policy" ON classes FOR SELECT USING (true);

DROP POLICY IF EXISTS "classes_insert_policy" ON classes;
CREATE POLICY "classes_insert_policy" ON classes FOR INSERT WITH CHECK (public.is_admin());

DROP POLICY IF EXISTS "classes_update_policy" ON classes;
CREATE POLICY "classes_update_policy" ON classes FOR UPDATE USING (public.is_admin());

-- ACHIEVEMENTS - Public read, admin write
DROP POLICY IF EXISTS "achievements_select_policy" ON achievements;
CREATE POLICY "achievements_select_policy" ON achievements FOR SELECT USING (true);

DROP POLICY IF EXISTS "achievements_insert_policy" ON achievements;
CREATE POLICY "achievements_insert_policy" ON achievements FOR INSERT WITH CHECK (public.is_admin());

-- SUBJECTS - Public read, lecturer/admin write
DROP POLICY IF EXISTS "subjects_select_policy" ON subjects;
CREATE POLICY "subjects_select_policy" ON subjects FOR SELECT USING (true);

DROP POLICY IF EXISTS "subjects_insert_policy" ON subjects;
CREATE POLICY "subjects_insert_policy" ON subjects FOR INSERT WITH CHECK (public.is_lecturer() OR public.is_admin());

DROP POLICY IF EXISTS "subjects_update_policy" ON subjects;
CREATE POLICY "subjects_update_policy" ON subjects FOR UPDATE USING (public.is_lecturer() OR public.is_admin());

-- CURRICULUM PROGRAMS - All authenticated users can view, Game Masters can manage
DROP POLICY IF EXISTS "curriculum_programs_select_policy" ON curriculum_programs;
CREATE POLICY "curriculum_programs_select_policy" ON curriculum_programs
  FOR SELECT USING (auth.uid() IS NOT NULL);

DROP POLICY IF EXISTS "curriculum_programs_insert_policy" ON curriculum_programs;
CREATE POLICY "curriculum_programs_insert_policy" ON curriculum_programs
  FOR INSERT WITH CHECK (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_programs_update_policy" ON curriculum_programs;
CREATE POLICY "curriculum_programs_update_policy" ON curriculum_programs
  FOR UPDATE USING (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_programs_delete_policy" ON curriculum_programs;
CREATE POLICY "curriculum_programs_delete_policy" ON curriculum_programs
  FOR DELETE USING (public.is_game_master());

-- CURRICULUM VERSIONS - All authenticated users can view, Game Masters can manage
DROP POLICY IF EXISTS "curriculum_versions_select_policy" ON curriculum_versions;
CREATE POLICY "curriculum_versions_select_policy" ON curriculum_versions
  FOR SELECT USING (auth.uid() IS NOT NULL);

DROP POLICY IF EXISTS "curriculum_versions_insert_policy" ON curriculum_versions;
CREATE POLICY "curriculum_versions_insert_policy" ON curriculum_versions
  FOR INSERT WITH CHECK (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_versions_update_policy" ON curriculum_versions;
CREATE POLICY "curriculum_versions_update_policy" ON curriculum_versions
  FOR UPDATE USING (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_versions_delete_policy" ON curriculum_versions;
CREATE POLICY "curriculum_versions_delete_policy" ON curriculum_versions
  FOR DELETE USING (public.is_game_master());

-- CURRICULUM STRUCTURE - All authenticated users can view, Game Masters can manage
DROP POLICY IF EXISTS "curriculum_structure_select_policy" ON curriculum_structure;
CREATE POLICY "curriculum_structure_select_policy" ON curriculum_structure
  FOR SELECT USING (auth.uid() IS NOT NULL);

DROP POLICY IF EXISTS "curriculum_structure_insert_policy" ON curriculum_structure;
CREATE POLICY "curriculum_structure_insert_policy" ON curriculum_structure
  FOR INSERT WITH CHECK (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_structure_update_policy" ON curriculum_structure;
CREATE POLICY "curriculum_structure_update_policy" ON curriculum_structure
  FOR UPDATE USING (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_structure_delete_policy" ON curriculum_structure;
CREATE POLICY "curriculum_structure_delete_policy" ON curriculum_structure
  FOR DELETE USING (public.is_game_master());

-- CURRICULUM VERSION ACTIVATIONS - All authenticated users can view, Game Masters can manage
DROP POLICY IF EXISTS "curriculum_version_activations_select_policy" ON curriculum_version_activations;
CREATE POLICY "curriculum_version_activations_select_policy" ON curriculum_version_activations
  FOR SELECT USING (auth.uid() IS NOT NULL);

DROP POLICY IF EXISTS "curriculum_version_activations_insert_policy" ON curriculum_version_activations;
CREATE POLICY "curriculum_version_activations_insert_policy" ON curriculum_version_activations
  FOR INSERT WITH CHECK (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_version_activations_update_policy" ON curriculum_version_activations;
CREATE POLICY "curriculum_version_activations_update_policy" ON curriculum_version_activations
  FOR UPDATE USING (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_version_activations_delete_policy" ON curriculum_version_activations;
CREATE POLICY "curriculum_version_activations_delete_policy" ON curriculum_version_activations
  FOR DELETE USING (public.is_game_master());

-- CURRICULUM IMPORT JOBS - Game Masters only
DROP POLICY IF EXISTS "curriculum_import_jobs_select_policy" ON curriculum_import_jobs;
CREATE POLICY "curriculum_import_jobs_select_policy" ON curriculum_import_jobs
  FOR SELECT USING (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_import_jobs_insert_policy" ON curriculum_import_jobs;
CREATE POLICY "curriculum_import_jobs_insert_policy" ON curriculum_import_jobs
  FOR INSERT WITH CHECK (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_import_jobs_update_policy" ON curriculum_import_jobs;
CREATE POLICY "curriculum_import_jobs_update_policy" ON curriculum_import_jobs
  FOR UPDATE USING (public.is_game_master());

DROP POLICY IF EXISTS "curriculum_import_jobs_delete_policy" ON curriculum_import_jobs;
CREATE POLICY "curriculum_import_jobs_delete_policy" ON curriculum_import_jobs
  FOR DELETE USING (public.is_game_master());

-- SYLLABUS VERSIONS - All authenticated users can view, elevated users can manage
DROP POLICY IF EXISTS "syllabus_versions_select_policy" ON syllabus_versions;
CREATE POLICY "syllabus_versions_select_policy" ON syllabus_versions
  FOR SELECT USING (auth.uid() IS NOT NULL);

DROP POLICY IF EXISTS "syllabus_versions_insert_policy" ON syllabus_versions;
CREATE POLICY "syllabus_versions_insert_policy" ON syllabus_versions
  FOR INSERT WITH CHECK (public.has_elevated_access());

DROP POLICY IF EXISTS "syllabus_versions_update_policy" ON syllabus_versions;
CREATE POLICY "syllabus_versions_update_policy" ON syllabus_versions
  FOR UPDATE USING (public.has_elevated_access());

DROP POLICY IF EXISTS "syllabus_versions_delete_policy" ON syllabus_versions;
CREATE POLICY "syllabus_versions_delete_policy" ON syllabus_versions
  FOR DELETE USING (public.is_game_master());

-- ELECTIVE PACKS - All authenticated users can view, elevated users can manage
DROP POLICY IF EXISTS "elective_packs_select_policy" ON elective_packs;
CREATE POLICY "elective_packs_select_policy" ON elective_packs
  FOR SELECT USING (auth.uid() IS NOT NULL);

DROP POLICY IF EXISTS "elective_packs_insert_policy" ON elective_packs;
CREATE POLICY "elective_packs_insert_policy" ON elective_packs
  FOR INSERT WITH CHECK (public.is_game_master());

DROP POLICY IF EXISTS "elective_packs_update_policy" ON elective_packs;
CREATE POLICY "elective_packs_update_policy" ON elective_packs
  FOR UPDATE USING (public.is_game_master());

DROP POLICY IF EXISTS "elective_packs_delete_policy" ON elective_packs;
CREATE POLICY "elective_packs_delete_policy" ON elective_packs
  FOR DELETE USING (public.is_game_master());

-- ELECTIVE SOURCES - All authenticated users can view, elevated users can manage
DROP POLICY IF EXISTS "elective_sources_select_policy" ON elective_sources;
CREATE POLICY "elective_sources_select_policy" ON elective_sources
  FOR SELECT USING (auth.uid() IS NOT NULL);

DROP POLICY IF EXISTS "elective_sources_insert_policy" ON elective_sources;
CREATE POLICY "elective_sources_insert_policy" ON elective_sources
  FOR INSERT WITH CHECK (public.is_game_master());

DROP POLICY IF EXISTS "elective_sources_update_policy" ON elective_sources;
CREATE POLICY "elective_sources_update_policy" ON elective_sources
  FOR UPDATE USING (public.is_game_master());

DROP POLICY IF EXISTS "elective_sources_delete_policy" ON elective_sources;
CREATE POLICY "elective_sources_delete_policy" ON elective_sources
  FOR DELETE USING (public.is_game_master());

-- Grant necessary permissions
-- =====================================================

-- Grant usage on auth schema
GRANT USAGE ON SCHEMA auth TO authenticated, anon;
GRANT USAGE ON SCHEMA public TO authenticated, anon;

-- Grant execute on helper functions
GRANT EXECUTE ON FUNCTION public.jwt_has_role(text) TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.user_has_role(text) TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_admin() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_game_master() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_lecturer() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_guild_master() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_party_leader() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_student() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_player() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.is_leader() TO authenticated, anon;
GRANT EXECUTE ON FUNCTION public.has_elevated_access() TO authenticated, anon;

-- Grant select on all tables to authenticated users (RLS will control access)
GRANT SELECT ON ALL TABLES IN SCHEMA public TO authenticated;
GRANT INSERT ON ALL TABLES IN SCHEMA public TO authenticated;
GRANT UPDATE ON ALL TABLES IN SCHEMA public TO authenticated;
GRANT DELETE ON ALL TABLES IN SCHEMA public TO authenticated;

-- Grant select on reference tables to anonymous users for public data
GRANT SELECT ON roles TO anon;
GRANT SELECT ON skills TO anon;
GRANT SELECT ON skill_dependencies TO anon;
GRANT SELECT ON classes TO anon;
GRANT SELECT ON achievements TO anon;
GRANT SELECT ON subjects TO anon;
GRANT SELECT ON curriculum_programs TO anon;
GRANT SELECT ON curriculum_versions TO anon;
GRANT SELECT ON curriculum_structure TO anon;
GRANT SELECT ON syllabus_versions TO anon;
GRANT SELECT ON elective_packs TO anon;
GRANT SELECT ON elective_sources TO anon;

-- =====================================================
-- End of RLS Policies Script
-- =====================================================