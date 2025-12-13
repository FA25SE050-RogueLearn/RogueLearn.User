-- Script: Storage Policies (Roadmap Imports)
-- Summary: Private bucket; Game Master read/write/delete

-- Bucket creation (idempotent)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'roadmap-imports',
    'roadmap-imports',
    false, -- Private bucket
    20971520, -- 20MB file size limit
    ARRAY['application/json', 'text/plain', 'application/pdf']
)
ON CONFLICT (id) DO NOTHING;

-- Enable RLS on objects
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Policies

-- Read: Game Masters only
DROP POLICY IF EXISTS "roadmap_imports_select_policy" ON storage.objects;
CREATE POLICY "roadmap_imports_select_policy" ON storage.objects
  FOR SELECT 
  USING (
    bucket_id = 'roadmap-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Insert: Game Masters only
DROP POLICY IF EXISTS "roadmap_imports_insert_policy" ON storage.objects;
CREATE POLICY "roadmap_imports_insert_policy" ON storage.objects
  FOR INSERT 
  WITH CHECK (
    bucket_id = 'roadmap-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Update: Game Masters only
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

-- Delete: Game Masters only
DROP POLICY IF EXISTS "roadmap_imports_delete_policy" ON storage.objects;
CREATE POLICY "roadmap_imports_delete_policy" ON storage.objects
  FOR DELETE 
  USING (
    bucket_id = 'roadmap-imports' 
    AND auth.uid() IS NOT NULL 
    AND public.jwt_has_role('Game Master')
  );

-- Grants

-- Schema usage
GRANT USAGE ON SCHEMA storage TO authenticated;

-- Buckets read
GRANT SELECT ON storage.buckets TO authenticated;

-- Objects CRUD (guarded by RLS)
GRANT SELECT, INSERT, UPDATE, DELETE ON storage.objects TO authenticated;

-- Notes: Path conventions enforced by application code