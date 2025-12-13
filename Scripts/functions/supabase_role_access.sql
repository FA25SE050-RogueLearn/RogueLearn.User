CREATE OR REPLACE FUNCTION public.jwt_has_role(role_name text) RETURNS boolean AS $$
  SELECT COALESCE((auth.jwt() -> 'roles') ? role_name, false);
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.user_has_role(role_name text) RETURNS boolean AS $$
  SELECT EXISTS (
    SELECT 1 FROM user_roles ur
    JOIN roles r ON ur.role_id = r.id
    WHERE ur.auth_user_id = auth.uid()
    AND r.name = role_name
  );
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_game_master() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Game Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_admin() RETURNS boolean AS $$
  SELECT public.is_game_master();
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_lecturer() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Verified Lecturer');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_guild_master() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Guild Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_party_leader() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Party Leader');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_player() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Player');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_student() RETURNS boolean AS $$
  SELECT public.is_player();
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.is_leader() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Party Leader') OR public.jwt_has_role('Guild Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;

CREATE OR REPLACE FUNCTION public.has_elevated_access() RETURNS boolean AS $$
  SELECT public.jwt_has_role('Verified Lecturer') OR public.jwt_has_role('Guild Master') OR public.jwt_has_role('Game Master');
$$ LANGUAGE sql STABLE SECURITY INVOKER;