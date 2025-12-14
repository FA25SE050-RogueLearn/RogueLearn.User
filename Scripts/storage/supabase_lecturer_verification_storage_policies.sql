-- Script: Storage Policies (Lecturer Verification)
-- Summary: Public read; user owns folder; Game Master admin

-- Bucket creation (idempotent)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'lecturer-verification',
    'lecturer-verification',
    true, -- Public bucket (proof links are served publicly)
    5242880, -- 5MB file size limit
    ARRAY['image/png','image/jpeg','image/webp','image/gif','application/pdf']
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
DROP POLICY IF EXISTS "lecturer_verification_public_read" ON storage.objects;
CREATE POLICY "lecturer_verification_public_read" ON storage.objects
  FOR SELECT
  USING (
    bucket_id = 'lecturer-verification'
  );

-- Insert: authenticated users, own folder only
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

-- Update: own folder or Game Master
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

-- Delete: own folder or Game Master
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

-- Notes: Allowed extensions enforced in insert/update policies