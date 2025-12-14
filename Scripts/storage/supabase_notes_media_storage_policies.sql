-- Script: Storage Policies (Notes Media)
-- Summary: Public read; authenticated users manage own folder; Game Master admin
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'notes-media',
  'notes-media',
  true,
  10485760,
  null
)
ON CONFLICT (id) DO NOTHING;

ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA storage TO authenticated, anon;
GRANT SELECT ON storage.buckets TO authenticated, anon;
GRANT SELECT ON storage.objects TO authenticated, anon;
GRANT INSERT, UPDATE, DELETE ON storage.objects TO authenticated;

DROP POLICY IF EXISTS "notes_media_public_read" ON storage.objects;
CREATE POLICY "notes_media_public_read" ON storage.objects
  FOR SELECT
  USING (
    bucket_id = 'notes-media'
  );

DROP POLICY IF EXISTS "notes_media_insert_own_folder" ON storage.objects;
CREATE POLICY "notes_media_insert_own_folder" ON storage.objects
  FOR INSERT TO authenticated
  WITH CHECK (
    bucket_id = 'notes-media'
    AND auth.uid() IS NOT NULL
    AND owner = auth.uid()
    AND (
      name LIKE auth.uid()::text || '/%'
      OR name = auth.uid()::text
    )
  );

DROP POLICY IF EXISTS "notes_media_update_own_or_admin" ON storage.objects;
CREATE POLICY "notes_media_update_own_or_admin" ON storage.objects
  FOR UPDATE TO authenticated
  USING (
    bucket_id = 'notes-media'
    AND (
      (
        owner = auth.uid()
        AND (
          name LIKE auth.uid()::text || '/%'
          OR name = auth.uid()::text
        )
      )
      OR public.jwt_has_role('Game Master')
    )
  )
  WITH CHECK (
    bucket_id = 'notes-media'
    AND (
      (
        owner = auth.uid()
        AND (
          name LIKE auth.uid()::text || '/%'
          OR name = auth.uid()::text
        )
      )
      OR public.jwt_has_role('Game Master')
    )
  );

DROP POLICY IF EXISTS "notes_media_delete_own_or_admin" ON storage.objects;
CREATE POLICY "notes_media_delete_own_or_admin" ON storage.objects
  FOR DELETE TO authenticated
  USING (
    bucket_id = 'notes-media'
    AND (
      (
        owner = auth.uid()
        AND (
          name LIKE auth.uid()::text || '/%'
          OR name = auth.uid()::text
        )
      )
      OR public.jwt_has_role('Game Master')
    )
  );