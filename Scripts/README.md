# RogueLearn SQL Scripts

Quick instructions to run Supabase/PostgreSQL scripts in this folder.

Copy-Paste Execution Order:
```
Scripts/database/supabase_enums.sql
Scripts/database/supabase_entities.sql
Scripts/database/supabase_index.sql
Scripts/functions/supabase_role_access.sql
Scripts/functions/supabase_custom_access_token_hook.sql
Scripts/functions/supabase_handle_new_user.sql
Scripts/functions/supabase_get_full_user_info.sql
Scripts/storage/supabase_achievements_storage_policies.sql  -- optional
Scripts/storage/supabase_user_avatars_storage_policies.sql  -- optional
Scripts/storage/supabase_guild_posts_storage_policies.sql   -- optional
Scripts/storage/supabase_notes_media_storage_policies.sql   -- optional
Scripts/storage/supabase_curriculum_storage_policies.sql    -- optional
Scripts/storage/supabase_roadmap_storage_policies.sql       -- optional
Scripts/seed/supabase_roles_seed.sql
Scripts/seed/supabase_achievements_seed.sql
```

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