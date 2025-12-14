-- Script: Storage Policies (Guild Posts)
-- Summary: Public read; authenticated write for guild-posts bucket
begin;

insert into storage.buckets (id, name, public)
values ('guild-posts', 'guild-posts', true)
on conflict (id) do update set public = excluded.public;

alter table if exists storage.objects enable row level security;

drop policy if exists guild_posts_read_public on storage.objects;
drop policy if exists guild_posts_insert_authenticated on storage.objects;
drop policy if exists guild_posts_update_authenticated on storage.objects;
drop policy if exists guild_posts_delete_authenticated on storage.objects;

create policy guild_posts_read_public
  on storage.objects for select
  using (bucket_id = 'guild-posts');

create policy guild_posts_insert_authenticated
  on storage.objects for insert
  to authenticated
  with check (bucket_id = 'guild-posts');

create policy guild_posts_update_authenticated
  on storage.objects for update
  to authenticated
  using (bucket_id = 'guild-posts')
  with check (bucket_id = 'guild-posts');

create policy guild_posts_delete_authenticated
  on storage.objects for delete
  to authenticated
  using (bucket_id = 'guild-posts');

commit;