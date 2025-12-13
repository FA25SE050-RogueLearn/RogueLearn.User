# RogueLearn RLS Policies Documentation

This directory contains SQL scripts for implementing Row-Level Security (RLS) policies for the RogueLearn platform on Supabase.

## Files

- **`supabase_rls_policies.sql`** - Main RLS policy implementation script
- **`test_rls_policies.sql`** - Test queries to verify RLS policies work correctly
- **`README.md`** - This documentation file

## Overview

### User Roles in RogueLearn

The RogueLearn platform uses a gamified role system:

1. **Player (Student)** - Primary users who select Routes (Academic Curriculum) and Classes (Career Specialization)
2. **Party Leader** - Students who lead small curriculum-compatible groups for focused academic collaboration
3. **Guild Master** - Students or verified Lecturers who create curriculum-focused communities
4. **Game Master (Admin)** - Privileged users responsible for technical oversight and educational content management
5. **Lecturer** - Verified educators who can become Guild Masters and manage educational content

### Entity Categories

#### User-Specific Data
- `user_profiles` - Player profile information
- `user_roles` - Role assignments for each player
- `user_skills` - Individual skill progress
- `user_skill_rewards` - Rewards earned by players
- `user_achievements` - Achievements unlocked by players
- `user_quest_progress` - Progress on curriculum-based quest lines
- `notifications` - Personal notifications

#### Academic Data
- `student_enrollments` - Route (curriculum) enrollments
- `student_semester_subjects` - Subject enrollments per term
- `lecturer_verification_requests` - Lecturer verification applications

#### Reference/System Data
- `roles` - Available roles in the system
- `skills` - Available skills and competencies
- `skill_dependencies` - Skill prerequisite relationships
- `classes` - Career specialization classes
- `achievements` - Available achievements
- `subjects` - Academic subjects
- `curriculum_programs` - Academic programs (Routes)
- `curriculum_versions` - Version control for curricula
- `curriculum_structure` - Curriculum organization
- `curriculum_version_activations` - Active curriculum versions
- `syllabus_versions` - Course syllabi
  

## Setup Instructions

1. **Run the main RLS script:**
   ```sql
   -- Execute in Supabase SQL Editor
   \i supabase_rls_policies.sql
   ```

2. **Create test users with appropriate roles:**
   ```sql
   -- Insert test users in user_profiles and user_roles tables
   -- See test_rls_policies.sql for examples
   ```

3. **Run the test script to verify policies:**
   ```sql
   \i test_rls_policies.sql
   ```

## Policy Details

### Access Patterns

#### Players (Students)
- **Own Data**: Full CRUD access to their personal records
- **Academic Data**: Can view and manage their own enrollments and progress
- **Reference Data**: Read-only access to curriculum, skills, and achievements
- **Guild/Party Data**: Access based on membership and leadership roles

#### Party Leaders
- **Enhanced Access**: Can manage party-related data for their groups
- **Academic Collaboration**: Can view relevant academic progress of party members
- **Resource Management**: Can organize shared learning resources

#### Guild Masters
- **Community Management**: Can manage guild-related data and membership
- **Academic Oversight**: Can monitor route-based student progress in their guilds
- **Event Creation**: Can create curriculum-aligned events for guild members
- **Content Management**: Can manage syllabus versions and educational content

#### Game Masters (Admins)
- **Full System Access**: Complete CRUD access to all entities
- **Content Management**: Can create and modify curriculum, skills, and achievements
- **User Management**: Can manage user roles and verification requests
- **System Administration**: Access to import jobs and system configurations

#### Lecturers
- **Educational Content**: Can manage syllabus versions and course content
- **Student Progress**: Can view and update academic progress for their courses
- **Guild Eligibility**: Can become Guild Masters to create educational communities
- **Verification**: Must go through verification process to gain full lecturer privileges

### Security Features

1. **Authentication Required**: All policies require valid authentication
2. **Role-Based Access**: Access controlled by user roles in `user_roles` table
3. **Data Isolation**: Users can only access their own personal data
4. **Hierarchical Permissions**: Higher roles inherit appropriate lower-level permissions
5. **Academic Context**: Access to academic data respects enrollment and teaching relationships

## Performance Considerations

- **Indexes**: Ensure proper indexes on `auth_user_id` and role-related columns
- **Role Caching**: Consider caching role checks for frequently accessed data
- **Query Optimization**: Monitor query performance, especially for complex role hierarchies

## Troubleshooting

### Common Issues

1. **No Data Returned**: Check if RLS is enabled and user has appropriate role
2. **Permission Denied**: Verify user authentication and role assignments
3. **Slow Queries**: Check indexes on filtered columns, especially `auth_user_id`

### Debugging Queries

```sql
-- Check if user is authenticated
SELECT auth.uid();

-- Check user roles
SELECT * FROM user_roles WHERE auth_user_id = auth.uid();

-- Check RLS status
SELECT schemaname, tablename, rowsecurity 
FROM pg_tables 
WHERE schemaname = 'public' AND rowsecurity = true;
```

## Maintenance

### Adding New Entities

When adding new entities to the system:

1. Enable RLS: `ALTER TABLE new_table ENABLE ROW LEVEL SECURITY;`
2. Create appropriate policies based on data sensitivity
3. Add test cases to verify policy behavior
4. Update this documentation

### Modifying Policies

When modifying existing policies:

1. Test changes in development environment first
2. Verify no data access is broken for existing users
3. Update test cases as needed
4. Document changes in version control

### Role Management

- New roles should be added to the `roles` table
- Update helper functions in the RLS script for new role checks
- Ensure proper role hierarchy and inheritance
- Test role transitions and permission changes

## Security Best Practices

1. **Principle of Least Privilege**: Grant minimum necessary access
2. **Regular Audits**: Periodically review and test policies
3. **Role Validation**: Ensure role assignments are properly validated
4. **Data Classification**: Classify data sensitivity and apply appropriate policies
5. **Monitoring**: Monitor for unusual access patterns or policy violations
# RogueLearn SQL Scripts

Quick instructions to run Supabase/PostgreSQL scripts in this folder.

## Structure
- `database/`: enums, entities, indexes
- `functions/`: role helpers, token hook, new user handler, user info
- `storage/`: bucket creation and policies (RLS on `storage.objects` only)
- `seed/`: initial data (roles, achievements)
- `migrations/`: incremental upgrades

## Apply Order
1. `database/supabase_enums.sql`
2. `database/supabase_entities.sql`
3. `database/supabase_index.sql`
4. `functions/*`
5. `storage/*` (optional)
6. `seed/*`
7. `migrations/*` as needed

## Run Options
### Supabase SQL Editor
- Open editor, paste file contents, execute in the order above.

### psql (PowerShell)
```powershell
$Opts = "-h <host> -U <user> -d <db>"
psql $Opts -f "Scripts\database\supabase_enums.sql"
psql $Opts -f "Scripts\database\supabase_entities.sql"
psql $Opts -f "Scripts\database\supabase_index.sql"
psql $Opts -f "Scripts\functions\supabase_role_access.sql"
psql $Opts -f "Scripts\functions\supabase_custom_access_token_hook.sql"
psql $Opts -f "Scripts\functions\supabase_handle_new_user.sql"
psql $Opts -f "Scripts\functions\supabase_get_full_user_info.sql"
psql $Opts -f "Scripts\storage\supabase_achievements_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_user_avatars_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_guild_posts_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_notes_media_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_curriculum_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_roadmap_storage_policies.sql"
psql $Opts -f "Scripts\seed\supabase_roles_seed.sql"
psql $Opts -f "Scripts\seed\supabase_achievements_seed.sql"
```

### Supabase CLI
```powershell
supabase db execute -f Scripts\database\supabase_enums.sql
supabase db execute -f Scripts\database\supabase_entities.sql
supabase db execute -f Scripts\database\supabase_index.sql
supabase db execute -f Scripts\functions\supabase_role_access.sql
supabase db execute -f Scripts\functions\supabase_custom_access_token_hook.sql
supabase db execute -f Scripts\functions\supabase_handle_new_user.sql
supabase db execute -f Scripts\functions\supabase_get_full_user_info.sql
supabase db execute -f Scripts\storage\supabase_achievements_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_user_avatars_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_guild_posts_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_notes_media_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_curriculum_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_roadmap_storage_policies.sql
supabase db execute -f Scripts\seed\supabase_roles_seed.sql
supabase db execute -f Scripts\seed\supabase_achievements_seed.sql
```

## Notes
- RLS is not globally enabled; storage policies enable RLS on `storage.objects` only.
- Run seeds after entities and indexes.
- Migrations are idempotent where possible.
# RogueLearn SQL Scripts

Quick instructions to run Supabase/PostgreSQL scripts in this folder.

## Structure
- `database/`: enums, entities, indexes
- `functions/`: role helpers, token hook, new user handler, user info
- `storage/`: bucket creation and policies (RLS on `storage.objects` only)
- `seed/`: initial data (roles, achievements)
- `migrations/`: incremental upgrades

## Apply Order
1. `database/supabase_enums.sql`
2. `database/supabase_entities.sql`
3. `database/supabase_index.sql`
4. `functions/*`
5. `storage/*` (optional)
6. `seed/*`
7. `migrations/*` as needed

## Run Options
### Supabase SQL Editor
- Open editor, paste file contents, execute in the order above.

### psql (PowerShell)
```powershell
$Opts = "-h <host> -U <user> -d <db>"
psql $Opts -f "Scripts\database\supabase_enums.sql"
psql $Opts -f "Scripts\database\supabase_entities.sql"
psql $Opts -f "Scripts\database\supabase_index.sql"
psql $Opts -f "Scripts\functions\supabase_role_access.sql"
psql $Opts -f "Scripts\functions\supabase_custom_access_token_hook.sql"
psql $Opts -f "Scripts\functions\supabase_handle_new_user.sql"
psql $Opts -f "Scripts\functions\supabase_get_full_user_info.sql"
psql $Opts -f "Scripts\storage\supabase_achievements_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_user_avatars_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_guild_posts_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_notes_media_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_curriculum_storage_policies.sql"
psql $Opts -f "Scripts\storage\supabase_roadmap_storage_policies.sql"
psql $Opts -f "Scripts\seed\supabase_roles_seed.sql"
psql $Opts -f "Scripts\seed\supabase_achievements_seed.sql"
```

### Supabase CLI
```powershell
supabase db execute -f Scripts\database\supabase_enums.sql
supabase db execute -f Scripts\database\supabase_entities.sql
supabase db execute -f Scripts\database\supabase_index.sql
supabase db execute -f Scripts\functions\supabase_role_access.sql
supabase db execute -f Scripts\functions\supabase_custom_access_token_hook.sql
supabase db execute -f Scripts\functions\supabase_handle_new_user.sql
supabase db execute -f Scripts\functions\supabase_get_full_user_info.sql
supabase db execute -f Scripts\storage\supabase_achievements_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_user_avatars_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_guild_posts_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_notes_media_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_curriculum_storage_policies.sql
supabase db execute -f Scripts\storage\supabase_roadmap_storage_policies.sql
supabase db execute -f Scripts\seed\supabase_roles_seed.sql
supabase db execute -f Scripts\seed\supabase_achievements_seed.sql
```

## Notes
- RLS is not globally enabled; storage policies enable RLS on `storage.objects` only.
- Run seeds after entities and indexes.
- Migrations are idempotent where possible.