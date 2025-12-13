-- Supabase Storage policies for 'achievements' bucket
-- This script will:
-- 1) Create (or update) the public 'achievements' bucket for achievement icon images
-- 2) Enable RLS on storage.objects
-- 3) Allow public read access to objects in the 'achievements' bucket
-- 4) Allow authenticated users to insert/update/delete objects in the 'achievements' bucket
--    (Service role key bypasses RLS and can always perform operations)

begin;

-- Create or update the bucket
insert into storage.buckets (id, name, public)
values ('achievements', 'achievements', true)
on conflict (id) do update set public = excluded.public;

-- Optionally constrain MIME types and file size limits (uncomment if desired)
-- update storage.buckets
--    set allowed_mime_types = array['image/png','image/jpeg','image/webp','image/svg+xml'],
--        file_size_limit = 10485760 -- 10 MB
--  where id = 'achievements';

-- Ensure RLS is enabled
alter table if exists storage.objects enable row level security;

-- Clean up any existing policies to avoid duplicates
drop policy if exists achievements_read_public on storage.objects;
drop policy if exists achievements_insert_authenticated on storage.objects;
drop policy if exists achievements_update_authenticated on storage.objects;
drop policy if exists achievements_delete_authenticated on storage.objects;

-- Public read for achievements bucket
create policy achievements_read_public
  on storage.objects for select
  using (bucket_id = 'achievements');

-- Authenticated users can insert into achievements bucket
create policy achievements_insert_authenticated
  on storage.objects for insert
  to authenticated
  with check (bucket_id = 'achievements');

-- Authenticated users can update objects in achievements bucket
create policy achievements_update_authenticated
  on storage.objects for update
  to authenticated
  using (bucket_id = 'achievements')
  with check (bucket_id = 'achievements');

-- Authenticated users can delete objects in achievements bucket
create policy achievements_delete_authenticated
  on storage.objects for delete
  to authenticated
  using (bucket_id = 'achievements');

commit;