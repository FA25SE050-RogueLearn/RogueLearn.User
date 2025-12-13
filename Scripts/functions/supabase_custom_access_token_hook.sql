CREATE OR REPLACE FUNCTION public.custom_access_token_hook(event jsonb)
RETURNS jsonb
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = ''
AS $$
DECLARE
    claims jsonb;
    user_roles text[];
BEGIN
    claims := event->'claims';

    SELECT array_agg(r.name)
    INTO user_roles
    FROM public.user_roles ur
    JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.auth_user_id = (event->>'user_id')::uuid;

    IF user_roles IS NOT NULL AND array_length(user_roles, 1) > 0 THEN
        claims := jsonb_set(claims, '{roles}', to_jsonb(user_roles));
    ELSE
        claims := jsonb_set(claims, '{roles}', '[]'::jsonb);
    END IF;

    event := jsonb_set(event, '{claims}', claims);
    RETURN event;
END;
$$;

GRANT USAGE ON SCHEMA public TO supabase_auth_admin;
GRANT EXECUTE ON FUNCTION public.custom_access_token_hook TO supabase_auth_admin;
REVOKE EXECUTE ON FUNCTION public.custom_access_token_hook FROM authenticated, anon, public;
GRANT ALL ON TABLE public.user_roles TO supabase_auth_admin;
GRANT ALL ON TABLE public.roles TO supabase_auth_admin;

CREATE POLICY "Allow auth admin to read user roles" ON public.user_roles
AS PERMISSIVE FOR SELECT
TO supabase_auth_admin
USING (true);

CREATE POLICY "Allow auth admin to read roles" ON public.roles
AS PERMISSIVE FOR SELECT
TO supabase_auth_admin
USING (true);

CREATE OR REPLACE FUNCTION public.authorize_role(required_role text)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = ''
AS $$
DECLARE
    user_roles text[];
BEGIN
    SELECT array(SELECT jsonb_array_elements_text(auth.jwt() -> 'roles'))
    INTO user_roles;
    RETURN required_role = ANY(user_roles);
END;
$$;