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
-- CURRICULUM STRUCTURE:
-- /curriculum/cache/{rawTextHash}.json                    - Cached curriculum JSON by hash
-- /curriculum/{programCode}/{versionCode}/latest.json     - Latest curriculum JSON
-- /curriculum/{programCode}/{versionCode}/latest.txt      - Latest raw text content  
-- /curriculum/{programCode}/{versionCode}/meta.json       - Latest metadata JSON
-- /curriculum/{programCode}/{versionCode}/{rawTextHash}.json - Versioned curriculum JSON by hash
--
-- SYLLABUS STRUCTURE:
-- /syllabus/_hashes/{rawTextHash}.json                    - Cached syllabus JSON by hash
-- /syllabus/{subjectCode}/{version}/latest.json           - Latest syllabus JSON
-- /syllabus/{subjectCode}/{version}/latest.meta.json      - Latest syllabus metadata JSON
-- /syllabus/{subjectCode}/{version}/versions/{rawTextHash}.json - Versioned syllabus JSON by hash
-- /syllabus/_temp/validation_{inputHash}/latest.json      - Temporary syllabus validation data
--
-- Examples:
-- /curriculum/cache/abc123def456.json
-- /curriculum/COMP-SCI/2024/latest.json
-- /curriculum/COMP-SCI/2024/latest.txt
-- /curriculum/COMP-SCI/2024/meta.json
-- /curriculum/COMP-SCI/2024/abc123def456.json
--
-- /syllabus/_hashes/def456ghi789.json
-- /syllabus/BIT_SE_K17D_K18A/2024/latest.json
-- /syllabus/BIT_SE_K17D_K18A/2024/latest.meta.json
-- /syllabus/BIT_SE_K17D_K18A/2024/versions/def456ghi789.json
-- /syllabus/_temp/validation_xyz789/latest.json
--
-- =====================================================
-- End of Storage Policies Script
-- =====================================================