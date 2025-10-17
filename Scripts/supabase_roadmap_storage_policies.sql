-- =====================================================
-- RogueLearn User Service - Supabase Storage Policies (Roadmap Imports)
-- =====================================================
-- This script creates storage policies for the roadmap-imports bucket
-- to ensure proper access control for roadmap import operations

-- Create the roadmap-imports bucket if it doesn't exist
-- =====================================================
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'roadmap-imports',
    'roadmap-imports',
    false, -- Private bucket
    20971520, -- 20MB file size limit
    ARRAY['application/json', 'text/plain', 'application/pdf']
)
ON CONFLICT (id) DO NOTHING;

-- Enable RLS on storage objects
-- =====================================================
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Storage Policies for roadmap-imports bucket
-- =====================================================

-- Policy for SELECT (read) operations
-- Only Game Masters can read from roadmap-imports bucket
DROP POLICY IF EXISTS "roadmap_imports_select_policy" ON storage.objects;
CREATE POLICY "roadmap_imports_select_policy" ON storage.objects
  FOR SELECT 
  USING (
    bucket_id = 'roadmap-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Policy for INSERT (upload) operations  
DROP POLICY IF EXISTS "roadmap_imports_insert_policy" ON storage.objects;
CREATE POLICY "roadmap_imports_insert_policy" ON storage.objects
  FOR INSERT 
  WITH CHECK (
    bucket_id = 'roadmap-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Policy for UPDATE operations
-- Only Game Masters can update objects in roadmap-imports bucket
DROP POLICY IF EXISTS "roadmap_imports_update_policy" ON storage.objects;
CREATE POLICY "roadmap_imports_update_policy" ON storage.objects
  FOR UPDATE 
  USING (
    bucket_id = 'roadmap-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  )
  WITH CHECK (
    bucket_id = 'roadmap-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Policy for DELETE operations
-- Only Game Masters can delete objects from roadmap-imports bucket
DROP POLICY IF EXISTS "roadmap_imports_delete_policy" ON storage.objects;
CREATE POLICY "roadmap_imports_delete_policy" ON storage.objects
  FOR DELETE 
  USING (
    bucket_id = 'roadmap-imports' 
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
-- File Path Structure for roadmap-imports bucket:
-- =====================================================
-- 
-- The following file structure is expected in the roadmap-imports bucket:
--
-- ROADMAP STRUCTURE:
-- /roadmap/_hashes/{rawTextHash}.json                        - Cached roadmap JSON by hash
-- /roadmap/{classSlug}/latest.json                           - Latest roadmap JSON
-- /roadmap/{classSlug}/latest.meta.json                      - Latest roadmap metadata JSON
-- /roadmap/{classSlug}/raw/latest.txt                        - Latest raw text content
-- /roadmap/{classSlug}/versions/{rawTextHash}.json           - Versioned roadmap JSON by hash
-- /roadmap/{classSlug}/attachments/{rawTextHash}.pdf         - Attached PDF file for the import (optional)
--
-- Examples:
-- /roadmap/_hashes/abc123def456.json
-- /roadmap/backend-developer/latest.json
-- /roadmap/backend-developer/latest.meta.json
-- /roadmap/backend-developer/raw/latest.txt
-- /roadmap/backend-developer/versions/abc123def456.json
-- /roadmap/backend-developer/attachments/abc123def456.pdf
--
-- =====================================================
-- End of Roadmap Storage Policies Script