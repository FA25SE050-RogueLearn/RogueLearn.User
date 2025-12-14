-- Script: Handle New User
-- Summary: Creates profile and assigns Player role after auth user signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
DECLARE
    student_role_id UUID;
    base_username TEXT;
    candidate_username TEXT;
    suffix INTEGER := 0;
BEGIN
    base_username := COALESCE(
        NULLIF(NEW.raw_user_meta_data->>'username', ''),
        NULLIF(split_part(NEW.email, '@', 1), ''),
        'user_' || substr(NEW.id::text, 1, 8)
    );

    candidate_username := base_username;

    WHILE EXISTS (SELECT 1 FROM public.user_profiles WHERE username = candidate_username) LOOP
        suffix := suffix + 1;
        candidate_username := base_username || '_' || suffix;
    END LOOP;

    INSERT INTO public.user_profiles (auth_user_id, email, username, first_name, last_name)
    VALUES (
        NEW.id,
        NEW.email,
        candidate_username,
        NEW.raw_user_meta_data->>'first_name',
        NEW.raw_user_meta_data->>'last_name'
    );

    SELECT id INTO student_role_id
    FROM public.roles
    WHERE name = 'Player'
    LIMIT 1;

    IF student_role_id IS NOT NULL THEN
        INSERT INTO public.user_roles (id, auth_user_id, role_id, assigned_at, assigned_by)
        VALUES (
            gen_random_uuid(),
            NEW.id,
            student_role_id,
            now(),
            NULL
        );
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Section: Trigger and grants
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

GRANT EXECUTE ON FUNCTION public.handle_new_user() TO supabase_auth_admin;
GRANT EXECUTE ON FUNCTION public.handle_new_user() TO authenticated;
GRANT EXECUTE ON FUNCTION public.handle_new_user() TO anon;
