-- =====================================================
-- RogueLearn User Service - Supabase Storage Policies (User Avatars)
-- =====================================================
-- This script creates a new public storage bucket `user-avatars` and
-- enforces access policies so that:
-- - Anyone can read public avatar files
-- - Each authenticated user can manage (upload/update/delete) files only inside their own folder `{auth_user_id}/...`
-- - Game Masters can manage all files as administrators
-- =====================================================

-- Create the user-avatars bucket if it doesn't exist
-- =====================================================
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'user-avatars',
    'user-avatars',
    true, -- Public bucket (so avatars can be publicly served via CDN)
    5242880, -- 5MB file size limit
    ARRAY['image/png','image/jpeg','image/webp','image/gif'] -- Allowed image types (excluding SVG for security)
)
ON CONFLICT (id) DO NOTHING;

-- Enable RLS on storage objects (global)
-- =====================================================
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Grants (RLS will still enforce row-level access)
-- =====================================================
GRANT USAGE ON SCHEMA storage TO authenticated, anon;
GRANT SELECT ON storage.buckets TO authenticated, anon;
GRANT SELECT ON storage.objects TO authenticated, anon;
GRANT INSERT, UPDATE, DELETE ON storage.objects TO authenticated;

-- Storage Policies for user-avatars bucket
-- =====================================================

-- Public Read: allow anyone to read files from user-avatars bucket
DROP POLICY IF EXISTS "user_avatars_public_read" ON storage.objects;
CREATE POLICY "user_avatars_public_read" ON storage.objects
  FOR SELECT
  USING (
    bucket_id = 'user-avatars'
  );

-- Upload: authenticated users can upload to their own folder only
-- Path must start with their auth.uid(), and object owner must be auth.uid()
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

-- Update: authenticated users can update files inside their own folder, or Game Masters can update any
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

-- Delete: authenticated users can delete files inside their own folder, or Game Masters can delete any
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

-- =====================================================
-- Recommended File Path Structure for user-avatars bucket:
-- =====================================================
-- Each user's files must live under a folder equal to their auth.uid().
-- Suggested canonical avatar file path (single current avatar per user):
-- /{auth_user_id}/avatar.png
-- or
-- /{auth_user_id}/avatar.webp
-- Optional versioned or multiple variants (thumbnails, etc.):
-- /{auth_user_id}/versions/{timestamp}.png
-- /{auth_user_id}/thumbnails/128x128.png
-- =====================================================