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
  ) order by s.semester nulls last) as data
  from student_semester_subjects sts
  left join subjects s on s.id = sts.subject_id
  where sts.auth_user_id = p_auth_user_id
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