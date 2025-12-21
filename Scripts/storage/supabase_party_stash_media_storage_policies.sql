INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'party-stash-media',
  'party-stash-media',
  false,
  10485760,
  null
)
ON CONFLICT (id) DO NOTHING;

ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA storage TO authenticated;
GRANT SELECT ON storage.buckets TO authenticated;
GRANT SELECT ON storage.objects TO authenticated;
GRANT INSERT, UPDATE, DELETE ON storage.objects TO authenticated;

DROP POLICY IF EXISTS "party_stash_member_read" ON storage.objects;
CREATE POLICY "party_stash_member_read" ON storage.objects
  FOR SELECT
  USING (
    bucket_id = 'party-stash-media'
    AND auth.uid() IS NOT NULL
    AND (
      EXISTS (
        SELECT 1 FROM public.party_members pm
        WHERE pm.party_id::text = split_part(name, '/', 1)
        AND pm.auth_user_id = auth.uid()
        AND pm.status = 'Active'
      )
      OR public.jwt_has_role('Game Master')
    )
  );

DROP POLICY IF EXISTS "party_stash_insert_member" ON storage.objects;
CREATE POLICY "party_stash_insert_member" ON storage.objects
  FOR INSERT TO authenticated
  WITH CHECK (
    bucket_id = 'party-stash-media'
    AND auth.uid() IS NOT NULL
    AND EXISTS (
      SELECT 1 FROM public.party_members pm
      WHERE pm.party_id::text = split_part(name, '/', 1)
      AND pm.auth_user_id = auth.uid()
      AND pm.status = 'Active'
    )
  );

DROP POLICY IF EXISTS "party_stash_update_member_or_admin" ON storage.objects;
CREATE POLICY "party_stash_update_member_or_admin" ON storage.objects
  FOR UPDATE TO authenticated
  USING (
    bucket_id = 'party-stash-media'
    AND auth.uid() IS NOT NULL
    AND (
      EXISTS (
        SELECT 1 FROM public.party_members pm
        WHERE pm.party_id::text = split_part(name, '/', 1)
        AND pm.auth_user_id = auth.uid()
        AND pm.status = 'Active'
      )
      OR public.jwt_has_role('Game Master')
    )
  )
  WITH CHECK (
    bucket_id = 'party-stash-media'
    AND auth.uid() IS NOT NULL
    AND (
      EXISTS (
        SELECT 1 FROM public.party_members pm
        WHERE pm.party_id::text = split_part(name, '/', 1)
        AND pm.auth_user_id = auth.uid()
        AND pm.status = 'Active'
      )
      OR public.jwt_has_role('Game Master')
    )
  );

DROP POLICY IF EXISTS "party_stash_delete_member_or_admin" ON storage.objects;
CREATE POLICY "party_stash_delete_member_or_admin" ON storage.objects
  FOR DELETE TO authenticated
  USING (
    bucket_id = 'party-stash-media'
    AND auth.uid() IS NOT NULL
    AND (
      EXISTS (
        SELECT 1 FROM public.party_members pm
        WHERE pm.party_id::text = split_part(name, '/', 1)
        AND pm.auth_user_id = auth.uid()
        AND pm.status = 'Active'
      )
      OR public.jwt_has_role('Game Master')
    )
  );