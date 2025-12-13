-- User profile indexes
CREATE INDEX idx_user_profiles_username ON user_profiles(username);
CREATE INDEX idx_user_profiles_email ON user_profiles(email);
CREATE INDEX idx_user_profiles_class_id ON user_profiles(class_id);

-- Roadmap.sh specialization indexes
CREATE INDEX idx_classes_name ON classes(name);
CREATE INDEX idx_classes_is_active ON classes(is_active);
CREATE INDEX idx_classes_difficulty_level ON classes(difficulty_level);
-- Class hierarchy and mapping indexes
CREATE INDEX idx_class_nodes_class_id ON class_nodes(class_id);
CREATE INDEX idx_class_nodes_parent_id ON class_nodes(parent_id);
CREATE INDEX idx_class_nodes_class_parent_seq ON class_nodes(class_id, parent_id, sequence);
CREATE INDEX idx_class_specialization_subjects_class_id ON class_specialization_subjects(class_id);
CREATE INDEX idx_class_specialization_subjects_subject_id ON class_specialization_subjects(subject_id);

-- Academic structure indexes
CREATE INDEX idx_curriculum_structure_curriculum_version_id ON curriculum_structure(curriculum_version_id);
CREATE INDEX idx_curriculum_structure_subject_id ON curriculum_structure(subject_id);
CREATE INDEX idx_student_enrollments_auth_user_id ON student_enrollments(auth_user_id);
CREATE INDEX idx_student_semester_subjects_enrollment_id ON student_semester_subjects(enrollment_id);

-- Notes/Arsenal indexes
CREATE INDEX idx_notes_auth_user_id ON notes(auth_user_id);
 
CREATE INDEX idx_tags_auth_user_id ON tags(auth_user_id);
CREATE INDEX idx_note_tags_note_id ON note_tags(note_id);
CREATE INDEX idx_note_tags_tag_id ON note_tags(tag_id);

-- Skills and progress indexes
CREATE INDEX idx_user_skills_auth_user_id ON user_skills(auth_user_id);
CREATE INDEX idx_user_quest_progress_auth_user_id ON user_quest_progress(auth_user_id);
CREATE INDEX idx_user_quest_progress_quest_id ON user_quest_progress(quest_id);
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

-- Admin governance indexes
CREATE INDEX idx_curriculum_version_activations_version_id ON curriculum_version_activations(curriculum_version_id);