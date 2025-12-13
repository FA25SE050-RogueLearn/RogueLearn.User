-- Script: Seed Achievements
-- Summary: Inserts predefined achievement records (idempotent by id)
INSERT INTO public.achievements (
  id, name, description, icon_url, source_service, key, rule_type, rule_config, category, version, is_active, merit_points_reward, contribution_points_reward, is_medal
) VALUES 
  ('3419e43b-e11f-44be-b570-f7671c2c0dc2', 'Code Battle Participant', 'Achievement is granted for those who participated in a Code Battle event', 'https://mmenecibrehzfpvblrrd.supabase.co/storage/v1/object/public/achievements/copper.png', 'Code Battle', 'code_battle_participant', NULL, NULL, NULL, NULL, TRUE, 10, 10, TRUE),
  ('5ff87bff-a9f4-43d8-a2e1-55dcab5b2c7f', 'Code Battle Winner Top 3', 'Achievement is granted for those who reached top 3 in a Code Battle event', 'https://mmenecibrehzfpvblrrd.supabase.co/storage/v1/object/public/achievements/silver.png', 'Code Battle', 'code_battle_top_3', NULL, NULL, NULL, NULL, TRUE, 100, 100, TRUE),
  ('8df7f392-f4c9-42a8-937b-ff09f2a62415', 'Code Battle Winner Top 1', 'Achievement is granted for those who reached top 1 in a Code Battle event', 'https://mmenecibrehzfpvblrrd.supabase.co/storage/v1/object/public/achievements/diamond.png', 'Code Battle', 'code_battle_top_1', NULL, NULL, NULL, NULL, TRUE, 300, 300, TRUE),
  ('d08908cf-60b6-42e4-be12-75a38ca6e010', 'Test achievement', 'Test achievement', NULL, 'RogueLearn', 'test', NULL, NULL, NULL, 1, TRUE, NULL, NULL, TRUE),
  ('f406d8ec-7e83-410b-b74c-dc09e2b52d5e', 'Code Battle Winner Top 2', 'Achievement is granted for those who reached top 2 in a Code Battle event', 'https://mmenecibrehzfpvblrrd.supabase.co/storage/v1/object/public/achievements/gold.png', 'Code Battle', 'code_battle_top_2', NULL, NULL, NULL, NULL, TRUE, 200, 200, TRUE)
ON CONFLICT (id) DO NOTHING;