-- Script: Storage Policies (User Avatars)
-- Summary: Public read; user owns folder; Game Master admin

-- Bucket creation (idempotent)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'user-avatars',
    'user-avatars',
    true, -- Public bucket (so avatars can be publicly served via CDN)
    5242880, -- 5MB file size limit
    ARRAY['image/png','image/jpeg','image/webp','image/gif'] -- Allowed image types (excluding SVG for security)
)
ON CONFLICT (id) DO NOTHING;

-- Enable RLS on objects
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Grants
GRANT USAGE ON SCHEMA storage TO authenticated, anon;
GRANT SELECT ON storage.buckets TO authenticated, anon;
GRANT SELECT ON storage.objects TO authenticated, anon;
GRANT INSERT, UPDATE, DELETE ON storage.objects TO authenticated;

-- Policies

-- Read: public
DROP POLICY IF EXISTS "user_avatars_public_read" ON storage.objects;
CREATE POLICY "user_avatars_public_read" ON storage.objects
  FOR SELECT
  USING (
    bucket_id = 'user-avatars'
  );

-- Insert: authenticated users, own folder only
DROP POLICY IF EXISTS "user_avatars_insert_own_folder" ON storage.objects;
CREATE POLICY "user_avatars_insert_own_folder" ON storage.objects
  FOR INSERT TO authenticated
  WITH CHECK (
    bucket_id = 'user-avatars'
    AND auth.uid() IS NOT NULL
    AND owner = auth.uid()
    AND (
      name LIKE auth.uid()::text || '/%'
      OR name = auth.uid()::text
    )
    AND COALESCE(storage.extension(name),'') IN ('png','jpg','jpeg','webp','gif')
  );

-- Update: own folder or Game Master
DROP POLICY IF EXISTS "user_avatars_update_own_or_admin" ON storage.objects;
CREATE POLICY "user_avatars_update_own_or_admin" ON storage.objects
  FOR UPDATE TO authenticated
  USING (
    bucket_id = 'user-avatars'
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
    bucket_id = 'user-avatars'
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
    AND COALESCE(storage.extension(name),'') IN ('png','jpg','jpeg','webp','gif')
  );

-- Delete: own folder or Game Master
DROP POLICY IF EXISTS "user_avatars_delete_own_or_admin" ON storage.objects;
CREATE POLICY "user_avatars_delete_own_or_admin" ON storage.objects
  FOR DELETE TO authenticated
  USING (
    bucket_id = 'user-avatars'
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

-- Notes: Store under {auth_user_id}/..., allowed extensions enforced in policies