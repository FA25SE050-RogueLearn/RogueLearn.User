-- =====================================================
-- RogueLearn User Service - Supabase Storage Policies
-- =====================================================
-- This script creates storage policies for the curriculum-imports bucket
-- to ensure proper access control for curriculum import operations

-- Create the curriculum-imports bucket if it doesn't exist
-- =====================================================
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'curriculum-imports',
    'curriculum-imports',
    false, -- Private bucket
    10485760, -- 10MB file size limit
    ARRAY['application/json', 'text/plain', 'text/json']
)
ON CONFLICT (id) DO NOTHING;

-- Enable RLS on storage objects
-- =====================================================
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Storage Policies for curriculum-imports bucket
-- =====================================================

-- Policy for SELECT (read) operations
-- Only Game Masters can read from curriculum-imports bucket
DROP POLICY IF EXISTS "curriculum_imports_select_policy" ON storage.objects;
CREATE POLICY "curriculum_imports_select_policy" ON storage.objects
  FOR SELECT 
  USING (
    bucket_id = 'curriculum-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Policy for INSERT (upload) operations  
-- Only Game Masters can upload to curriculum-imports bucket
DROP POLICY IF EXISTS "curriculum_imports_insert_policy" ON storage.objects;
CREATE POLICY "curriculum_imports_insert_policy" ON storage.objects
  FOR INSERT 
  WITH CHECK (
    bucket_id = 'curriculum-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Policy for UPDATE operations
-- Only Game Masters can update objects in curriculum-imports bucket
DROP POLICY IF EXISTS "curriculum_imports_update_policy" ON storage.objects;
CREATE POLICY "curriculum_imports_update_policy" ON storage.objects
  FOR UPDATE 
  USING (
    bucket_id = 'curriculum-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  )
  WITH CHECK (
    bucket_id = 'curriculum-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Policy for DELETE operations
-- Only Game Masters can delete objects from curriculum-imports bucket
DROP POLICY IF EXISTS "curriculum_imports_delete_policy" ON storage.objects;
CREATE POLICY "curriculum_imports_delete_policy" ON storage.objects
  FOR DELETE 
  USING (
    bucket_id = 'curriculum-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Grant necessary permissions for storage operations
-- =====================================================

-- Grant usage on storage schema to authenticated users
GRANT USAGE ON SCHEMA storage TO authenticated;

-- Grant select on buckets table to authenticated users (needed for bucket operations)
GRANT SELECT ON storage.buckets TO authenticated;

-- Grant all operations on objects table to authenticated users (RLS will control access)
GRANT SELECT, INSERT, UPDATE, DELETE ON storage.objects TO authenticated;

-- =====================================================
-- File Path Structure for curriculum-imports bucket:
-- =====================================================
-- 
-- The following file structure is expected in the curriculum-imports bucket:
--
-- /cache/{rawTextHash}.json                    - Cached curriculum JSON by hash
-- /{programCode}/{versionCode}/latest.json     - Latest curriculum JSON
-- /{programCode}/{versionCode}/latest.txt      - Latest raw text content  
-- /{programCode}/{versionCode}/meta.json       - Latest metadata JSON
-- /{programCode}/{versionCode}/{rawTextHash}.json - Versioned curriculum JSON by hash
--
-- Examples:
-- /cache/abc123def456.json
-- /COMP-SCI/2024/latest.json
-- /COMP-SCI/2024/latest.txt
-- /COMP-SCI/2024/meta.json
-- /COMP-SCI/2024/abc123def456.json
--
-- =====================================================
-- End of Storage Policies Script
-- =====================================================