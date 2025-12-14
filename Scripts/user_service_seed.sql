-- =====================================================
-- RogueLearn Database Seed Data Script
-- Summary: Comprehensive seed data for development/testing
-- =====================================================

-- =====================================================
-- Roles & Achievements DATA
-- =====================================================

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

-- Script: Seed Roles
-- Summary: Inserts core RBAC roles (idempotent by id)
INSERT INTO public.roles (id, name, description, permissions, created_at, updated_at) VALUES
('0973d594-8c29-4977-987b-8687e62fb922', 'Player', 'The primary user of the application. They select a Route (Academic Curriculum) and Class (Career Specialization), complete AI-driven gap analysis, pursue curriculum-based quest lines enhanced with career development content, manage their academic resources ("Arsenal"), and engage with curriculum-compatible collaborative learning features.', NULL, '2025-10-31 12:55:47.872586+00', '2025-10-31 12:55:47.872586+00'),
('189bb10c-2c9c-44a2-b214-c3675d4467de', 'Guild Master', 'Any user—either a Student or a verified Lecturer—can become a Guild Master by creating a "Guild." Guilds are curriculum-focused communities centered on specific Routes (academic programs) or interdisciplinary learning, similar to a academic department or professional learning community. The Guild Master is responsible for curriculum-based guild management, monitoring route-based student progress, and creating curriculum-aligned Events for their members.', NULL, '2025-10-31 12:56:30.627861+00', '2025-10-31 12:56:30.627861+00'),
('1e304ae7-5108-4d5d-a1bc-87b34ca4748a', 'Party Leader', 'A Student who takes on a leadership role within a "Party"—a small, curriculum-compatible group for focused academic collaboration, much like a study circle or project team. Party Leaders manage Route-compatible membership, organize curriculum-focused study sessions, and facilitate shared learning resource management.', NULL, '2025-10-31 12:56:11.769452+00', '2025-10-31 12:56:11.769452+00'),
('7e3ad9b1-7850-4ce5-8420-07cf831a4860', 'Verified Lecturer', NULL, NULL, '2025-10-31 12:57:07.020019+00', '2025-10-31 12:57:07.020019+00'),
('813ee032-24ca-4aea-a28a-c28b18c777e3', 'Game Master', 'A privileged user responsible for the technical oversight and educational content management of the RogueLearn platform. They create curriculum-aligned foundational content for competitive Events (like academic assessments and career readiness challenges), manage curriculum-career integration systems, and oversee the platform''s educational effectiveness.', NULL, '2025-10-31 12:56:44.64087+00', '2025-10-31 12:56:44.64087+00')
ON CONFLICT (id) DO NOTHING;

-- =====================================================
-- CURRICULUM DATA
-- =====================================================

-- Seed: Curriculum Programs (Academic Routes)
INSERT INTO curriculum_programs (id, program_name, program_code, description, degree_level, total_credits, duration_years) VALUES
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', 'Computer Science', 'CS-BSCS', 'Comprehensive program covering software development, algorithms, and computer systems', 'Bachelor', 120, 4),
('b2c3d4e5-f6a7-4890-b2c3-d4e5f6a78901', 'Information Technology', 'IT-BSIT', 'Focus on IT infrastructure, networking, and system administration', 'Bachelor', 120, 4),
('c3d4e5f6-a7b8-4901-c3d4-e5f6a7b89012', 'Data Science', 'DS-BSDS', 'Data analysis, machine learning, and statistical modeling', 'Bachelor', 120, 4),
('d4e5f6a7-b8c9-4012-d4e5-f6a7b8c90123', 'Software Engineering', 'SE-BSSE', 'Software development lifecycle, architecture, and project management', 'Bachelor', 120, 4),
('e5f6a7b8-c9d0-4123-e5f6-a7b8c9d01234', 'Cybersecurity', 'CYB-BSCY', 'Network security, cryptography, and ethical hacking', 'Bachelor', 120, 4),
('f6a7b8c9-d0e1-4234-f6a7-b8c9d0e12345', 'Computer Science', 'CS-MSCS', 'Advanced computer science topics and research', 'Master', 36, 2),
('a7b8c9d0-e1f2-4345-a7b8-c9d0e1f23456', 'Artificial Intelligence', 'AI-MSAI', 'Machine learning, neural networks, and AI systems', 'Master', 36, 2)
ON CONFLICT (id) DO NOTHING;

-- Seed: Classes (Career Specializations)
INSERT INTO classes (id, name, description, roadmap_url, skill_focus_areas, difficulty_level, estimated_duration_months, is_active) VALUES
('11111111-1111-1111-1111-111111111111', 'Full Stack Developer', 'Master both frontend and backend development', 'https://roadmap.sh/full-stack', ARRAY['React', 'Node.js', 'PostgreSQL', 'REST APIs', 'Git'], 'Intermediate', 18, TRUE),
('22222222-2222-2222-2222-222222222222', 'Frontend Developer', 'Specialize in modern frontend technologies', 'https://roadmap.sh/frontend', ARRAY['HTML', 'CSS', 'JavaScript', 'React', 'TypeScript'], 'Beginner', 12, TRUE),
('33333333-3333-3333-3333-333333333333', 'Backend Developer', 'Focus on server-side development', 'https://roadmap.sh/backend', ARRAY['Node.js', 'Python', 'SQL', 'API Design', 'Docker'], 'Intermediate', 15, TRUE),
('44444444-4444-4444-4444-444444444444', 'DevOps Engineer', 'Infrastructure and deployment automation', 'https://roadmap.sh/devops', ARRAY['Docker', 'Kubernetes', 'CI/CD', 'AWS', 'Terraform'], 'Advanced', 24, TRUE),
('55555555-5555-5555-5555-555555555555', 'Data Engineer', 'Build and maintain data pipelines', 'https://roadmap.sh/data-engineer', ARRAY['Python', 'SQL', 'ETL', 'Apache Spark', 'Airflow'], 'Advanced', 20, TRUE),
('66666666-6666-6666-6666-666666666666', 'Machine Learning Engineer', 'Develop and deploy ML models', 'https://roadmap.sh/mlops', ARRAY['Python', 'TensorFlow', 'PyTorch', 'MLOps', 'Statistics'], 'Expert', 24, TRUE),
('77777777-7777-7777-7777-777777777777', 'Mobile Developer', 'Create cross-platform mobile applications', 'https://roadmap.sh/react-native', ARRAY['React Native', 'JavaScript', 'Mobile UI/UX', 'Firebase', 'App Store'], 'Intermediate', 16, TRUE),
('88888888-8888-8888-8888-888888888888', 'Game Developer', 'Design and develop interactive games', 'https://roadmap.sh/game-developer', ARRAY['Unity', 'C#', 'Game Physics', '3D Graphics', 'AI'], 'Advanced', 22, TRUE),
('99999999-9999-9999-9999-999999999999', 'Blockchain Developer', 'Build decentralized applications', 'https://roadmap.sh/blockchain', ARRAY['Solidity', 'Web3', 'Smart Contracts', 'Ethereum', 'DeFi'], 'Expert', 20, TRUE),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'QA Engineer', 'Ensure software quality through testing', 'https://roadmap.sh/qa', ARRAY['Testing', 'Selenium', 'Jest', 'CI/CD', 'Test Automation'], 'Intermediate', 14, TRUE)
ON CONFLICT (id) DO NOTHING;

-- Seed: Subjects
INSERT INTO subjects (id, subject_code, subject_name, credits, description) VALUES
-- Core Computer Science
('10000001-0001-0001-0001-000000000001', 'CS101', 'Introduction to Programming', 3, 'Fundamentals of programming using Python'),
('10000002-0002-0002-0002-000000000002', 'CS102', 'Data Structures', 3, 'Arrays, linked lists, trees, and graphs'),
('10000003-0003-0003-0003-000000000003', 'CS201', 'Algorithms', 3, 'Algorithm design and analysis'),
('10000004-0004-0004-0004-000000000004', 'CS202', 'Database Systems', 3, 'Relational databases and SQL'),
('10000005-0005-0005-0005-000000000005', 'CS301', 'Operating Systems', 3, 'Process management and memory allocation'),
('10000006-0006-0006-0006-000000000006', 'CS302', 'Computer Networks', 3, 'Network protocols and architecture'),
('10000007-0007-0007-0007-000000000007', 'CS401', 'Software Engineering', 3, 'SDLC and project management'),
('10000008-0008-0008-0008-000000000008', 'CS402', 'Artificial Intelligence', 3, 'AI fundamentals and machine learning'),

-- Web Development
('10000009-0009-0009-0009-000000000009', 'WEB101', 'HTML & CSS Fundamentals', 3, 'Web page structure and styling'),
('10000010-0010-0010-0010-000000000010', 'WEB201', 'JavaScript Programming', 3, 'Client-side scripting and DOM manipulation'),
('10000011-0011-0011-0011-000000000011', 'WEB301', 'Frontend Frameworks', 3, 'React, Vue, and modern frontend development'),
('10000012-0012-0012-0012-000000000012', 'WEB302', 'Backend Development', 3, 'Server-side programming with Node.js'),

-- Data Science
('10000013-0013-0013-0013-000000000013', 'DS101', 'Statistics for Data Science', 3, 'Probability and statistical analysis'),
('10000014-0014-0014-0014-000000000014', 'DS201', 'Machine Learning', 3, 'Supervised and unsupervised learning'),
('10000015-0015-0015-0015-000000000015', 'DS301', 'Deep Learning', 3, 'Neural networks and deep learning frameworks'),
('10000016-0016-0016-0016-000000000016', 'DS302', 'Data Visualization', 3, 'Creating effective data visualizations'),

-- Cybersecurity
('10000017-0017-0017-0017-000000000017', 'CYB101', 'Introduction to Cybersecurity', 3, 'Security fundamentals and threats'),
('10000018-0018-0018-0018-000000000018', 'CYB201', 'Network Security', 3, 'Securing network infrastructure'),
('10000019-0019-0019-0019-000000000019', 'CYB301', 'Ethical Hacking', 3, 'Penetration testing and security assessment'),
('10000020-0020-0020-0020-000000000020', 'CYB302', 'Cryptography', 3, 'Encryption and cryptographic protocols'),

-- Mathematics & General
('10000021-0021-0021-0021-000000000021', 'MATH101', 'Calculus I', 3, 'Differential calculus'),
('10000022-0022-0022-0022-000000000022', 'MATH201', 'Linear Algebra', 3, 'Matrices and vector spaces'),
('10000023-0023-0023-0023-000000000023', 'MATH301', 'Discrete Mathematics', 3, 'Logic, sets, and combinatorics'),
('10000024-0024-0024-0024-000000000024', 'ENG101', 'Technical Writing', 3, 'Professional communication skills')
ON CONFLICT (id) DO NOTHING;

-- Seed: Curriculum Program Subjects Mapping
INSERT INTO curriculum_program_subjects (program_id, subject_id) VALUES
-- Computer Science Program
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000001-0001-0001-0001-000000000001'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000002-0002-0002-0002-000000000002'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000003-0003-0003-0003-000000000003'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000004-0004-0004-0004-000000000004'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000005-0005-0005-0005-000000000005'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000006-0006-0006-0006-000000000006'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000007-0007-0007-0007-000000000007'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000021-0021-0021-0021-000000000021'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000022-0022-0022-0022-000000000022'),
('a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890', '10000023-0023-0023-0023-000000000023'),

-- Data Science Program
('c3d4e5f6-a7b8-4901-c3d4-e5f6a7b89012', '10000001-0001-0001-0001-000000000001'),
('c3d4e5f6-a7b8-4901-c3d4-e5f6a7b89012', '10000013-0013-0013-0013-000000000013'),
('c3d4e5f6-a7b8-4901-c3d4-e5f6a7b89012', '10000014-0014-0014-0014-000000000014'),
('c3d4e5f6-a7b8-4901-c3d4-e5f6a7b89012', '10000015-0015-0015-0015-000000000015'),
('c3d4e5f6-a7b8-4901-c3d4-e5f6a7b89012', '10000016-0016-0016-0016-000000000016'),
('c3d4e5f6-a7b8-4901-c3d4-e5f6a7b89012', '10000022-0022-0022-0022-000000000022'),

-- Cybersecurity Program
('e5f6a7b8-c9d0-4123-e5f6-a7b8c9d01234', '10000017-0017-0017-0017-000000000017'),
('e5f6a7b8-c9d0-4123-e5f6-a7b8c9d01234', '10000018-0018-0018-0018-000000000018'),
('e5f6a7b8-c9d0-4123-e5f6-a7b8c9d01234', '10000019-0019-0019-0019-000000000019'),
('e5f6a7b8-c9d0-4123-e5f6-a7b8c9d01234', '10000020-0020-0020-0020-000000000020'),
('e5f6a7b8-c9d0-4123-e5f6-a7b8c9d01234', '10000006-0006-0006-0006-000000000006')
ON CONFLICT (program_id, subject_id) DO NOTHING;

-- Seed: Class Specialization Subjects
INSERT INTO class_specialization_subjects (class_id, subject_id, placeholder_subject_code, semester) VALUES
-- Full Stack Developer
('11111111-1111-1111-1111-111111111111', '10000009-0009-0009-0009-000000000009', 'WEB101', 1),
('11111111-1111-1111-1111-111111111111', '10000010-0010-0010-0010-000000000010', 'WEB201', 2),
('11111111-1111-1111-1111-111111111111', '10000011-0011-0011-0011-000000000011', 'WEB301', 3),
('11111111-1111-1111-1111-111111111111', '10000012-0012-0012-0012-000000000012', 'WEB302', 3),
('11111111-1111-1111-1111-111111111111', '10000004-0004-0004-0004-000000000004', 'CS202', 2),

-- Frontend Developer
('22222222-2222-2222-2222-222222222222', '10000009-0009-0009-0009-000000000009', 'WEB101', 1),
('22222222-2222-2222-2222-222222222222', '10000010-0010-0010-0010-000000000010', 'WEB201', 2),
('22222222-2222-2222-2222-222222222222', '10000011-0011-0011-0011-000000000011', 'WEB301', 3),

-- Backend Developer
('33333333-3333-3333-3333-333333333333', '10000001-0001-0001-0001-000000000001', 'CS101', 1),
('33333333-3333-3333-3333-333333333333', '10000012-0012-0012-0012-000000000012', 'WEB302', 2),
('33333333-3333-3333-3333-333333333333', '10000004-0004-0004-0004-000000000004', 'CS202', 2)
ON CONFLICT (id) DO NOTHING;

-- =====================================================
-- SKILLS DATA
-- =====================================================

INSERT INTO skills (id, name, domain, tier, description) VALUES
-- Programming Languages
('20000001-0001-0001-0001-000000000001', 'Python', 'Programming Languages', 'Foundation', 'General-purpose programming language'),
('20000002-0002-0002-0002-000000000002', 'JavaScript', 'Programming Languages', 'Foundation', 'Web programming language'),
('20000003-0003-0003-0003-000000000003', 'TypeScript', 'Programming Languages', 'Intermediate', 'Typed superset of JavaScript'),
('20000004-0004-0004-0004-000000000004', 'Java', 'Programming Languages', 'Foundation', 'Object-oriented programming language'),
('20000005-0005-0005-0005-000000000005', 'C++', 'Programming Languages', 'Intermediate', 'High-performance programming language'),
('20000006-0006-0006-0006-000000000006', 'Go', 'Programming Languages', 'Intermediate', 'Concurrent programming language'),
('20000007-0007-0007-0007-000000000007', 'Rust', 'Programming Languages', 'Advanced', 'Memory-safe systems programming'),
('20000008-0008-0008-0008-000000000008', 'SQL', 'Databases', 'Foundation', 'Database query language'),

-- Frontend Technologies
('20000009-0009-0009-0009-000000000009', 'HTML', 'Frontend', 'Foundation', 'Markup language for web pages'),
('20000010-0010-0010-0010-000000000010', 'CSS', 'Frontend', 'Foundation', 'Styling language for web pages'),
('20000011-0011-0011-0011-000000000011', 'React', 'Frontend', 'Intermediate', 'JavaScript library for building UIs'),
('20000012-0012-0012-0012-000000000012', 'Vue.js', 'Frontend', 'Intermediate', 'Progressive JavaScript framework'),
('20000013-0013-0013-0013-000000000013', 'Angular', 'Frontend', 'Intermediate', 'TypeScript-based web framework'),
('20000014-0014-0014-0014-000000000014', 'Tailwind CSS', 'Frontend', 'Intermediate', 'Utility-first CSS framework'),

-- Backend Technologies
('20000015-0015-0015-0015-000000000015', 'Node.js', 'Backend', 'Intermediate', 'JavaScript runtime for server-side'),
('20000016-0016-0016-0016-000000000016', 'Express.js', 'Backend', 'Intermediate', 'Web framework for Node.js'),
('20000017-0017-0017-0017-000000000017', 'Django', 'Backend', 'Intermediate', 'Python web framework'),
('20000018-0018-0018-0018-000000000018', 'Flask', 'Backend', 'Intermediate', 'Lightweight Python web framework'),
('20000019-0019-0019-0019-000000000019', 'REST API Design', 'Backend', 'Intermediate', 'RESTful API architecture'),
('20000020-0020-0020-0020-000000000020', 'GraphQL', 'Backend', 'Advanced', 'Query language for APIs'),

-- Databases
('20000021-0021-0021-0021-000000000021', 'PostgreSQL', 'Databases', 'Intermediate', 'Advanced relational database'),
('20000022-0022-0022-0022-000000000022', 'MongoDB', 'Databases', 'Intermediate', 'NoSQL document database'),
('20000023-0023-0023-0023-000000000023', 'Redis', 'Databases', 'Intermediate', 'In-memory data store'),
('20000024-0024-0024-0024-000000000024', 'MySQL', 'Databases', 'Foundation', 'Popular relational database'),

-- DevOps & Cloud
('20000025-0025-0025-0025-000000000025', 'Docker', 'DevOps', 'Intermediate', 'Containerization platform'),
('20000026-0026-0026-0026-000000000026', 'Kubernetes', 'DevOps', 'Advanced', 'Container orchestration'),
('20000027-0027-0027-0027-000000000027', 'AWS', 'Cloud', 'Intermediate', 'Amazon Web Services'),
('20000028-0028-0028-0028-000000000028', 'Azure', 'Cloud', 'Intermediate', 'Microsoft Azure cloud platform'),
('20000029-0029-0029-0029-000000000029', 'CI/CD', 'DevOps', 'Intermediate', 'Continuous integration and deployment'),
('20000030-0030-0030-0030-000000000030', 'Terraform', 'DevOps', 'Advanced', 'Infrastructure as code'),

-- Data Science & ML
('20000031-0031-0031-0031-000000000031', 'Machine Learning', 'Data Science', 'Advanced', 'ML algorithms and models'),
('20000032-0032-0032-0032-000000000032', 'TensorFlow', 'Data Science', 'Advanced', 'Deep learning framework'),
('20000033-0033-0033-0033-000000000033', 'PyTorch', 'Data Science', 'Advanced', 'Machine learning library'),
('20000034-0034-0034-0034-000000000034', 'Data Analysis', 'Data Science', 'Intermediate', 'Analyzing and interpreting data'),
('20000035-0035-0035-0035-000000000035', 'Statistics', 'Data Science', 'Intermediate', 'Statistical methods and theory'),

-- Tools & Practices
('20000036-0036-0036-0036-000000000036', 'Git', 'Version Control', 'Foundation', 'Version control system'),
('20000037-0037-0037-0037-000000000037', 'GitHub', 'Version Control', 'Foundation', 'Git hosting platform'),
('20000038-0038-0038-0038-000000000038', 'Testing', 'Software Engineering', 'Intermediate', 'Software testing practices'),
('20000039-0039-0039-0039-000000000039', 'Agile', 'Project Management', 'Intermediate', 'Agile methodologies'),
('20000040-0040-0040-0040-000000000040', 'System Design', 'Software Engineering', 'Advanced', 'Designing scalable systems')
ON CONFLICT (id) DO NOTHING;

-- Seed: Subject-Skill Mappings
INSERT INTO subject_skill_mappings (subject_id, skill_id, relevance_weight) VALUES
-- CS101 - Introduction to Programming
('10000001-0001-0001-0001-000000000001', '20000001-0001-0001-0001-000000000001', 1.00),
('10000001-0001-0001-0001-000000000001', '20000036-0036-0036-0036-000000000036', 0.70),

-- WEB101 - HTML & CSS
('10000009-0009-0009-0009-000000000009', '20000009-0009-0009-0009-000000000009', 1.00),
('10000009-0009-0009-0009-000000000009', '20000010-0010-0010-0010-000000000010', 1.00),

-- WEB201 - JavaScript
('10000010-0010-0010-0010-000000000010', '20000002-0002-0002-0002-000000000002', 1.00),
('10000010-0010-0010-0010-000000000010', '20000009-0009-0009-0009-000000000009', 0.50),
('10000010-0010-0010-0010-000000000010', '20000010-0010-0010-0010-000000000010', 0.50),

-- WEB301 - Frontend Frameworks
('10000011-0011-0011-0011-000000000011', '20000011-0011-0011-0011-000000000011', 1.00),
('10000011-0011-0011-0011-000000000011', '20000003-0003-0003-0003-000000000003', 0.80),
('10000011-0011-0011-0011-000000000011', '20000014-0014-0014-0014-000000000014', 0.70),

-- WEB302 - Backend Development
('10000012-0012-0012-0012-000000000012', '20000015-0015-0015-0015-000000000015', 1.00),
('10000012-0012-0012-0012-000000000012', '20000016-0016-0016-0016-000000000016', 0.90),
('10000012-0012-0012-0012-000000000012', '20000019-0019-0019-0019-000000000019', 0.80),

-- CS202 - Database Systems
('10000004-0004-0004-0004-000000000004', '20000008-0008-0008-0008-000000000008', 1.00),
('10000004-0004-0004-0004-000000000004', '20000021-0021-0021-0021-000000000021', 0.90),
('10000004-0004-0004-0004-000000000004', '20000024-0024-0024-0024-000000000024', 0.80),

-- DS201 - Machine Learning
('10000014-0014-0014-0014-000000000014', '20000031-0031-0031-0031-000000000031', 1.00),
('10000014-0014-0014-0014-000000000014', '20000001-0001-0001-0001-000000000001', 0.90),
('10000014-0014-0014-0014-000000000014', '20000035-0035-0035-0035-000000000035', 0.80)
ON CONFLICT (subject_id, skill_id) DO NOTHING;

-- Seed: Skill Dependencies
INSERT INTO skill_dependencies (skill_id, prerequisite_skill_id, relationship_type) VALUES
-- JavaScript prerequisites HTML/CSS
('20000002-0002-0002-0002-000000000002', '20000009-0009-0009-0009-000000000009', 'Prerequisite'),
('20000002-0002-0002-0002-000000000002', '20000010-0010-0010-0010-000000000010', 'Prerequisite'),

-- TypeScript requires JavaScript
('20000003-0003-0003-0003-000000000003', '20000002-0002-0002-0002-000000000002', 'Prerequisite'),

-- React requires JavaScript
('20000011-0011-0011-0011-000000000011', '20000002-0002-0002-0002-000000000002', 'Prerequisite'),

-- Node.js requires JavaScript
('20000015-0015-0015-0015-000000000015', '20000002-0002-0002-0002-000000000002', 'Prerequisite'),

-- Express requires Node.js
('20000016-0016-0016-0016-000000000016', '20000015-0015-0015-0015-000000000015', 'Prerequisite'),

-- Kubernetes requires Docker
('20000026-0026-0026-0026-000000000026', '20000025-0025-0025-0025-000000000025', 'Prerequisite'),

-- ML frameworks require Python
('20000032-0032-0032-0032-000000000032', '20000001-0001-0001-0001-000000000001', 'Prerequisite'),
('20000033-0033-0033-0033-000000000033', '20000001-0001-0001-0001-000000000001', 'Prerequisite'),

-- Machine Learning requires Statistics
('20000031-0031-0031-0031-000000000031', '20000035-0035-0035-0035-000000000035', 'Prerequisite')
ON CONFLICT (skill_id, prerequisite_skill_id, relationship_type) DO NOTHING;

-- =====================================================
-- QUESTS AND LEARNING PATHS (NO USER DEPENDENCIES)
-- =====================================================

-- Note: created_by fields are set to NULL since they reference user_profiles
-- You can update these later once you have actual users

-- Seed: Quests
INSERT INTO quests (id, title, description, quest_type, difficulty_level, estimated_duration_minutes, experience_points_reward, sequence, skill_tags, subject_id, is_active, created_by) VALUES
-- HTML/CSS Quests
('30000001-0001-0001-0001-000000000001', 'Build Your First Webpage', 'Create a simple HTML page with proper structure', 'Tutorial', 'Beginner', 60, 100, 1, ARRAY['HTML', 'Web Development'], '10000009-0009-0009-0009-000000000009', TRUE, NULL),
('30000002-0002-0002-0002-000000000002', 'Style with CSS', 'Apply CSS styling to your webpage', 'Tutorial', 'Beginner', 90, 150, 2, ARRAY['CSS', 'Web Development'], '10000009-0009-0009-0009-000000000009', TRUE, NULL),
('30000003-0003-0003-0003-000000000003', 'Responsive Design Challenge', 'Make your page work on all devices', 'Challenge', 'Intermediate', 120, 250, 3, ARRAY['CSS', 'Responsive Design'], '10000009-0009-0009-0009-000000000009', TRUE, NULL),

-- JavaScript Quests
('30000004-0004-0004-0004-000000000004', 'JavaScript Basics', 'Learn variables, functions, and control flow', 'Tutorial', 'Beginner', 90, 150, 1, ARRAY['JavaScript'], '10000010-0010-0010-0010-000000000010', TRUE, NULL),
('30000005-0005-0005-0005-000000000005', 'DOM Manipulation', 'Interact with HTML elements using JavaScript', 'Practice', 'Intermediate', 120, 200, 2, ARRAY['JavaScript', 'DOM'], '10000010-0010-0010-0010-000000000010', TRUE, NULL),
('30000006-0006-0006-0006-000000000006', 'Build a To-Do App', 'Create an interactive to-do list application', 'Project', 'Intermediate', 180, 400, 3, ARRAY['JavaScript', 'DOM', 'CSS'], '10000010-0010-0010-0010-000000000010', TRUE, NULL),

-- React Quests
('30000007-0007-0007-0007-000000000007', 'React Components 101', 'Understanding components and props', 'Tutorial', 'Intermediate', 120, 250, 1, ARRAY['React', 'JavaScript'], '10000011-0011-0011-0011-000000000011', TRUE, NULL),
('30000008-0008-0008-0008-000000000008', 'State Management', 'Learn React hooks and state', 'Tutorial', 'Intermediate', 150, 300, 2, ARRAY['React', 'State Management'], '10000011-0011-0011-0011-000000000011', TRUE, NULL),
('30000009-0009-0009-0009-000000000009', 'React Router Challenge', 'Build a multi-page React application', 'Challenge', 'Advanced', 180, 500, 3, ARRAY['React', 'Routing'], '10000011-0011-0011-0011-000000000011', TRUE, NULL),

-- Backend Quests
('30000010-0010-0010-0010-000000000010', 'Node.js Basics', 'Introduction to server-side JavaScript', 'Tutorial', 'Intermediate', 90, 200, 1, ARRAY['Node.js', 'Backend'], '10000012-0012-0012-0012-000000000012', TRUE, NULL),
('30000011-0011-0011-0011-000000000011', 'REST API Development', 'Build a RESTful API with Express', 'Project', 'Intermediate', 240, 600, 2, ARRAY['Node.js', 'Express', 'REST API'], '10000012-0012-0012-0012-000000000012', TRUE, NULL),
('30000012-0012-0012-0012-000000000012', 'Database Integration', 'Connect your API to PostgreSQL', 'Tutorial', 'Advanced', 180, 400, 3, ARRAY['Node.js', 'PostgreSQL', 'SQL'], '10000012-0012-0012-0012-000000000012', TRUE, NULL),

-- Database Quests
('30000013-0013-0013-0013-000000000013', 'SQL Fundamentals', 'Learn SELECT, INSERT, UPDATE, DELETE', 'Tutorial', 'Beginner', 120, 200, 1, ARRAY['SQL', 'Database'], '10000004-0004-0004-0004-000000000004', TRUE, NULL),
('30000014-0014-0014-0014-000000000014', 'Advanced SQL Queries', 'Master JOINs and subqueries', 'Practice', 'Intermediate', 150, 300, 2, ARRAY['SQL', 'Database'], '10000004-0004-0004-0004-000000000004', TRUE, NULL),
('30000015-0015-0015-0015-000000000015', 'Database Design Project', 'Design a normalized database schema', 'Project', 'Advanced', 240, 500, 3, ARRAY['SQL', 'Database Design'], '10000004-0004-0004-0004-000000000004', TRUE, NULL),

-- Python & Data Science
('30000016-0016-0016-0016-000000000016', 'Python Basics', 'Variables, functions, and control structures', 'Tutorial', 'Beginner', 90, 150, 1, ARRAY['Python'], '10000001-0001-0001-0001-000000000001', TRUE, NULL),
('30000017-0017-0017-0017-000000000017', 'Data Analysis with Pandas', 'Learn data manipulation with Pandas', 'Tutorial', 'Intermediate', 180, 350, 1, ARRAY['Python', 'Pandas', 'Data Science'], '10000014-0014-0014-0014-000000000014', TRUE, NULL),
('30000018-0018-0018-0018-000000000018', 'Machine Learning Basics', 'Build your first ML model', 'Tutorial', 'Advanced', 240, 600, 2, ARRAY['Python', 'Machine Learning'], '10000014-0014-0014-0014-000000000014', TRUE, NULL)
ON CONFLICT (id) DO NOTHING;

-- Seed: Quest Steps
INSERT INTO quest_steps (id, quest_id, step_number, title, description, step_type, experience_points, is_optional) VALUES
-- Quest 1: Build Your First Webpage
('40000001-0001-0001-0001-000000000001', '30000001-0001-0001-0001-000000000001', 1, 'HTML Document Structure', 'Learn about the basic HTML document structure', 'Reading', 20, FALSE),
('40000002-0002-0002-0002-000000000002', '30000001-0001-0001-0001-000000000001', 2, 'Create HTML File', 'Create your first HTML file with proper structure', 'Coding', 40, FALSE),
('40000003-0003-0003-0003-000000000003', '30000001-0001-0001-0001-000000000001', 3, 'Add Content', 'Add headings, paragraphs, and lists', 'Coding', 40, FALSE),

-- Quest 4: JavaScript Basics
('40000004-0004-0004-0004-000000000004', '30000004-0004-0004-0004-000000000004', 1, 'Variables and Data Types', 'Understanding variables and primitive types', 'Video', 30, FALSE),
('40000005-0005-0005-0005-000000000005', '30000004-0004-0004-0004-000000000004', 2, 'Functions in JavaScript', 'Creating and using functions', 'Interactive', 40, FALSE),
('40000006-0006-0006-0006-000000000006', '30000004-0004-0004-0004-000000000004', 3, 'Control Flow', 'If statements, loops, and conditionals', 'Coding', 50, FALSE),
('40000007-0007-0007-0007-000000000007', '30000004-0004-0004-0004-000000000004', 4, 'Practice Exercise', 'Complete JavaScript exercises', 'Quiz', 30, FALSE),

-- Quest 6: Build a To-Do App
('40000008-0008-0008-0008-000000000008', '30000006-0006-0006-0006-000000000006', 1, 'Project Planning', 'Design the to-do app structure', 'Reading', 40, FALSE),
('40000009-0009-0009-0009-000000000009', '30000006-0006-0006-0006-000000000006', 2, 'HTML Structure', 'Create the HTML structure', 'Coding', 80, FALSE),
('40000010-0010-0010-0010-000000000010', '30000006-0006-0006-0006-000000000006', 3, 'Add Interactivity', 'Implement JavaScript functionality', 'Coding', 120, FALSE),
('40000011-0011-0011-0011-000000000011', '30000006-0006-0006-0006-000000000006', 4, 'Styling', 'Make it look good with CSS', 'Coding', 80, FALSE),
('40000012-0012-0012-0012-000000000012', '30000006-0006-0006-0006-000000000006', 5, 'Submit Project', 'Submit your completed to-do app', 'Submission', 80, FALSE),

-- Quest 11: REST API Development
('40000013-0013-0013-0013-000000000013', '30000011-0011-0011-0011-000000000011', 1, 'REST Principles', 'Understanding RESTful architecture', 'Video', 60, FALSE),
('40000014-0014-0014-0014-000000000014', '30000011-0011-0011-0011-000000000011', 2, 'Setup Express', 'Initialize Node.js project with Express', 'Coding', 80, FALSE),
('40000015-0015-0015-0015-000000000015', '30000011-0011-0011-0011-000000000011', 3, 'Create Routes', 'Implement CRUD endpoints', 'Coding', 150, FALSE),
('40000016-0016-0016-0016-000000000016', '30000011-0011-0011-0011-000000000011', 4, 'Error Handling', 'Add proper error handling', 'Coding', 100, FALSE),
('40000017-0017-0017-0017-000000000017', '30000011-0011-0011-0011-000000000011', 5, 'Testing', 'Write API tests', 'Coding', 110, FALSE),
('40000018-0018-0018-0018-000000000018', '30000011-0011-0011-0011-000000000011', 6, 'Deploy API', 'Deploy your API to production', 'Submission', 100, TRUE)
ON CONFLICT (id) DO NOTHING;