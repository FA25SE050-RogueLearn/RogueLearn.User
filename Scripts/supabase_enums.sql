-- User management enums
CREATE TYPE user_role_type AS ENUM ('Student', 'Lecturer', 'Admin', 'SuperAdmin');
CREATE TYPE verification_status AS ENUM ('Pending', 'Approved', 'Rejected');
CREATE TYPE enrollment_status AS ENUM ('Active', 'Inactive', 'Graduated', 'Dropped', 'Suspended');
CREATE TYPE semester_status AS ENUM ('Enrolled', 'Completed', 'Failed', 'Withdrawn');
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
CREATE TYPE resource_type AS ENUM ('Document', 'Video', 'Audio', 'Interactive', 'Link', 'Code', 'Dataset');
CREATE TYPE quest_attempt_status AS ENUM ('InProgress', 'Completed', 'Abandoned', 'Paused');
CREATE TYPE step_completion_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Skipped');
CREATE TYPE path_type AS ENUM ('Course', 'Specialization', 'Bootcamp', 'Custom');
CREATE TYPE path_progress_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Paused');
CREATE TYPE assessment_type AS ENUM ('Quiz', 'Assignment', 'Project', 'PeerReview', 'AutoGraded', 'ManualReview');
CREATE TYPE quest_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Abandoned');

-- Social interaction enums
CREATE TYPE party_type AS ENUM ('StudyGroup', 'ProjectTeam', 'PeerReview', 'Casual', 'Competition');
CREATE TYPE party_role AS ENUM ('Leader', 'CoLeader', 'Member');
CREATE TYPE guild_role AS ENUM ('GuildMaster', 'Officer', 'Veteran', 'Member', 'Recruit');
CREATE TYPE member_status AS ENUM ('Active', 'Inactive', 'Suspended', 'Left');
CREATE TYPE invitation_status AS ENUM ('Pending', 'Accepted', 'Declined', 'Expired', 'Cancelled');
CREATE TYPE invitation_type AS ENUM ('Invite', 'Application');
CREATE TYPE guild_type AS ENUM ('Academic', 'Professional', 'Hobby', 'Competition', 'Study', 'Research');
CREATE TYPE activity_type AS ENUM ('QuestCompletion', 'StudySession', 'ProjectWork', 'Discussion', 'Competition', 'Meeting');
CREATE TYPE post_type AS ENUM ('announcement', 'discussion', 'general', 'achievement');
CREATE TYPE friendship_status AS ENUM ('Pending', 'Accepted', 'Blocked');

-- Guild join request enums
CREATE TYPE guild_join_request_status AS ENUM ('Pending', 'Accepted', 'Declined', 'Expired', 'Cancelled');

-- Meeting management enums
CREATE TYPE transcript_segment_status AS ENUM ('Processed', 'Failed');

-- Guild post moderation/status enums
CREATE TYPE guild_post_status AS ENUM ('published', 'pending', 'rejected');