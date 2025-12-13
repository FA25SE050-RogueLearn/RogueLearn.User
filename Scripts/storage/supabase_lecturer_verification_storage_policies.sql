-- =====================================================
-- RogueLearn User Service - Supabase Storage Policies (Lecturer Verification)
-- =====================================================
-- This script creates a new public storage bucket `lecturer-verification` and
-- enforces access policies so that:
-- - Anyone can read public proof files
-- - Each authenticated user can manage (upload/update/delete) files only inside their own folder `{auth_user_id}/...`
-- - Game Masters can manage all files as administrators
-- =====================================================

-- Create the lecturer-verification bucket if it doesn't exist
-- =====================================================
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'lecturer-verification',
    'lecturer-verification',
    true, -- Public bucket (proof links are served publicly)
    5242880, -- 5MB file size limit
    ARRAY['image/png','image/jpeg','image/webp','image/gif','application/pdf']
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

-- Storage Policies for lecturer-verification bucket
-- =====================================================

-- Public Read: allow anyone to read files from lecturer-verification bucket
DROP POLICY IF EXISTS "lecturer_verification_public_read" ON storage.objects;
CREATE POLICY "lecturer_verification_public_read" ON storage.objects
  FOR SELECT
  USING (
    bucket_id = 'lecturer-verification'
  );

-- Upload: authenticated users can upload to their own folder only
-- Path must start with their auth.uid(), and object owner must be auth.uid()
DROP POLICY IF EXISTS "lecturer_verification_insert_own_folder" ON storage.objects;
CREATE POLICY "lecturer_verification_insert_own_folder" ON storage.objects
  FOR INSERT TO authenticated
  WITH CHECK (
    bucket_id = 'lecturer-verification'
    AND auth.uid() IS NOT NULL
    AND owner = auth.uid()
    AND (
      name LIKE auth.uid()::text || '/%'
      OR name = auth.uid()::text
    )
    AND COALESCE(storage.extension(name),'') IN ('png','jpg','jpeg','webp','gif','pdf')
  );

-- Update: authenticated users can update files inside their own folder, or Game Masters can update any
DROP POLICY IF EXISTS "lecturer_verification_update_own_or_admin" ON storage.objects;
CREATE POLICY "lecturer_verification_update_own_or_admin" ON storage.objects
  FOR UPDATE TO authenticated
  USING (
    bucket_id = 'lecturer-verification'
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
    bucket_id = 'lecturer-verification'
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
    AND COALESCE(storage.extension(name),'') IN ('png','jpg','jpeg','webp','gif','pdf')
  );

-- Delete: authenticated users can delete files inside their own folder, or Game Masters can delete any
DROP POLICY IF EXISTS "lecturer_verification_delete_own_or_admin" ON storage.objects;
CREATE POLICY "lecturer_verification_delete_own_or_admin" ON storage.objects
  FOR DELETE TO authenticated
  USING (
    bucket_id = 'lecturer-verification'
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
-- Recommended File Path Structure for lecturer-verification bucket:
-- =====================================================
-- Each user's files must live under a folder equal to their auth.uid().
-- Proof files will be stored as:
-- /{auth_user_id}/{random-guid}.{ext}
-- Allowed extensions: png, jpg, jpeg, webp, gif, pdf
-- =====================================================