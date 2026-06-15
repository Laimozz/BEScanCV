-- USERS (5 rows)
INSERT INTO users (full_name, email, password_hash, role, status, created_at, updated_at) VALUES
('Nguyen Van An',     'an.nguyen@email.com',   '$2a$12$dummyhash1', 'HR',        'ACTIVE',   NOW(), NOW()),
('Tran Thi Bich',     'bich.tran@email.com',   '$2a$12$dummyhash2', 'HR',        'ACTIVE',   NOW(), NOW()),
('Le Van Cuong',      'cuong.le@email.com',     '$2a$12$dummyhash3', 'CANDIDATE', 'ACTIVE',   NOW(), NOW()),
('Pham Thi Dung',     'dung.pham@email.com',    '$2a$12$dummyhash4', 'CANDIDATE', 'ACTIVE',   NOW(), NOW()),
('Hoang Van Em',      'em.hoang@email.com',     '$2a$12$dummyhash5', 'ADMIN',     'ACTIVE',   NOW(), NOW());

-- REFRESH_TOKENS (5 rows)
INSERT INTO refresh_tokens (user_id, token_hash, expires_at, revoked_at, created_at) VALUES
(1, 'hash_token_001', NOW() + INTERVAL '7 days', NULL, NOW()),
(2, 'hash_token_002', NOW() + INTERVAL '7 days', NULL, NOW()),
(3, 'hash_token_003', NOW() + INTERVAL '7 days', NULL, NOW()),
(4, 'hash_token_004', NOW() + INTERVAL '7 days', NULL, NOW()),
(5, 'hash_token_005', NOW() + INTERVAL '7 days', NULL, NOW());

-- CV_FILES (6 rows)
INSERT INTO cv_files (uploaded_by, original_file_name, file_url, file_type, file_size, ai_document_id, created_at, updated_at) VALUES
(3, 'cv_le_van_cuong.pdf',    'https://storage.example.com/cvs/cv_001.pdf', 'pdf', 204800,  'ai_doc_001', NOW(), NOW()),
(3, 'cv_cuong_v2.pdf',        'https://storage.example.com/cvs/cv_002.pdf', 'pdf', 189440,  'ai_doc_002', NOW(), NOW()),
(4, 'cv_pham_thi_dung.pdf',   'https://storage.example.com/cvs/cv_003.pdf', 'pdf', 256000,  'ai_doc_003', NOW(), NOW()),
(4, 'cv_dung_update.docx',    'https://storage.example.com/cvs/cv_004.docx','docx',174080, 'ai_doc_004', NOW(), NOW()),
(1, 'cv_candidate_005.pdf',   'https://storage.example.com/cvs/cv_005.pdf', 'pdf', 220160,  'ai_doc_005', NOW(), NOW()),
(2, 'cv_candidate_006.pdf',   'https://storage.example.com/cvs/cv_006.pdf', 'pdf', 235520,  'ai_doc_006', NOW(), NOW());

-- CV_INFOS (6 rows, each row maps to one cv_file)
INSERT INTO cv_infos (
    cv_file_id,
    full_name,
    email,
    phone,
    date_of_birth,
    address,
    summary,
    educations,
    total_experience_years,
    raw_text,
    profile_data,
    status,
    position,
    created_at,
    updated_at
) VALUES
(1, 'Le Van Cuong', 'cuong.le@email.com', '0901234561', '1995-03-15', 'Ha Noi',
 'Backend developer 3 years experience with .NET',
 '[{"school":"Dai hoc Bach Khoa HN","major":"CNTT","graduation_year":2017}]'::jsonb,
 3,
 'Le Van Cuong. Backend Developer. Backend developer 3 years experience with .NET. Education: Dai hoc Bach Khoa HN - CNTT 2017. Certifications: AWS Certified Developer. Skills: C#, ASP.NET Core, PostgreSQL.',
 '{"certifications":["AWS Certified Developer"],"skills":["C#","ASP.NET Core","PostgreSQL"],"source":"seed.sql"}'::jsonb,
 'ACTIVE', 'Backend Developer', NOW(), NOW()),
(2, 'Le Van Cuong', 'cuong.le@email.com', '0901234561', '1995-03-15', 'Ha Noi',
 'Backend developer with microservices experience',
 '[{"school":"Dai hoc Bach Khoa HN","major":"CNTT","graduation_year":2017}]'::jsonb,
 4,
 'Le Van Cuong. Senior Backend Developer. Backend developer with microservices experience. Education: Dai hoc Bach Khoa HN - CNTT 2017. Certifications: AWS Certified Developer, Azure Fundamentals. Skills: C#, Docker.',
 '{"certifications":["AWS Certified Developer","Azure Fundamentals"],"skills":["C#","Docker"],"source":"seed.sql"}'::jsonb,
 'ACTIVE', 'Senior Backend Developer', NOW(), NOW()),
(3, 'Pham Thi Dung', 'dung.pham@email.com', '0901234562', '1997-07-20', 'Ho Chi Minh',
 'Frontend developer specializing in React and TypeScript',
 '[{"school":"Dai hoc KHTN HCM","major":"CNTT","graduation_year":2019}]'::jsonb,
 3,
 'Pham Thi Dung. Frontend Developer. Frontend developer specializing in React and TypeScript. Education: Dai hoc KHTN HCM - CNTT 2019. Certifications: Meta Front-End Developer Certificate. Skills: React, TypeScript.',
 '{"certifications":["Meta Front-End Developer Certificate"],"skills":["React","TypeScript"],"source":"seed.sql"}'::jsonb,
 'ACTIVE', 'Frontend Developer', NOW(), NOW()),
(4, 'Pham Thi Dung', 'dung.pham@email.com', '0901234562', '1997-07-20', 'Ho Chi Minh',
 'Fullstack developer React and Node.js',
 '[{"school":"Dai hoc KHTN HCM","major":"CNTT","graduation_year":2019}]'::jsonb,
 2,
 'Pham Thi Dung. Fullstack Developer. Fullstack developer React and Node.js. Education: Dai hoc KHTN HCM - CNTT 2019. Certifications: Meta Front-End Developer Certificate. Skills: Node.js.',
 '{"certifications":["Meta Front-End Developer Certificate"],"skills":["Node.js"],"source":"seed.sql"}'::jsonb,
 'ACTIVE', 'Fullstack Developer', NOW(), NOW()),
(5, 'Nguyen Minh Khoa', 'khoa.nm@email.com', '0901234563', '1993-11-05', 'Da Nang',
 'DevOps engineer with CI/CD and Docker experience',
 '[{"school":"Dai hoc Da Nang","major":"Ky thuat phan mem","graduation_year":2015}]'::jsonb,
 3,
 'Nguyen Minh Khoa. DevOps Engineer. DevOps engineer with CI/CD and Docker experience. Education: Dai hoc Da Nang - Ky thuat phan mem 2015. Certifications: CKA - Certified Kubernetes Administrator. Skills: Kubernetes.',
 '{"certifications":["CKA - Certified Kubernetes Administrator"],"skills":["Kubernetes"],"source":"seed.sql"}'::jsonb,
 'ACTIVE', 'DevOps Engineer', NOW(), NOW()),
(6, 'Vo Thi Lan', 'lan.vo@email.com', '0901234564', '1999-01-30', 'Can Tho',
 'Data analyst with Python, SQL and Power BI skills',
 '[{"school":"Dai hoc Can Tho","major":"He thong thong tin","graduation_year":2021}]'::jsonb,
 2,
 'Vo Thi Lan. Data Analyst. Data analyst with Python, SQL and Power BI skills. Education: Dai hoc Can Tho - He thong thong tin 2021. Certifications: Google Data Analytics Certificate. Skills: Python.',
 '{"certifications":["Google Data Analytics Certificate"],"skills":["Python"],"source":"seed.sql"}'::jsonb,
 'ACTIVE', 'Data Analyst', NOW(), NOW());

-- CV_SKILLS (10 rows)
INSERT INTO cv_skills (cv_infos_id, name) VALUES
(1, 'C#'),
(1, 'ASP.NET Core'),
(1, 'PostgreSQL'),
(2, 'C#'),
(2, 'Docker'),
(3, 'React'),
(3, 'TypeScript'),
(4, 'Node.js'),
(5, 'Kubernetes'),
(6, 'Python');
