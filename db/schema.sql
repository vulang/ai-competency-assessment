-- PostgreSQL schema for the AI competency assessment ERD.
-- Keep this file in sync with any future migrations.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE frameworks (
    framework_id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    version VARCHAR(64),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE domains (
    domain_id SERIAL PRIMARY KEY,
    framework_id INT NOT NULL REFERENCES frameworks(framework_id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    weight NUMERIC(5, 2) NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX ux_domains_framework_name ON domains (framework_id, name);

CREATE TABLE skills (
    skill_id SERIAL PRIMARY KEY,
    domain_id INT NOT NULL REFERENCES domains(domain_id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    weight NUMERIC(5, 2) NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX ux_skills_domain_name ON skills (domain_id, name);

CREATE TABLE criteria (
    criteria_id SERIAL PRIMARY KEY,
    skill_id INT NOT NULL REFERENCES skills(skill_id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    level INT NOT NULL CHECK (level BETWEEN 1 AND 5),
    description TEXT
);

CREATE UNIQUE INDEX ux_criteria_skill_name_level ON criteria (skill_id, name, level);

CREATE TABLE roles (
    role_id SERIAL PRIMARY KEY,
    role_name VARCHAR(64) NOT NULL UNIQUE,
    permissions JSONB NOT NULL DEFAULT '{}'::jsonb
);

CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(150) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role_id INT NOT NULL REFERENCES roles(role_id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE questions (
    question_id SERIAL PRIMARY KEY,
    skill_id INT REFERENCES skills(skill_id) ON DELETE SET NULL,
    type VARCHAR(32) NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    difficulty_level INT CHECK (difficulty_level BETWEEN 1 AND 5),
    rubric_scoring TEXT
);

CREATE INDEX ix_questions_skill_id ON questions (skill_id);

CREATE TABLE exams (
    exam_id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    duration_minutes INT NOT NULL CHECK (duration_minutes > 0),
    pass_score INT NOT NULL CHECK (pass_score >= 0),
    is_published BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE exam_questions (
    eq_id SERIAL PRIMARY KEY,
    exam_id INT NOT NULL REFERENCES exams(exam_id) ON DELETE CASCADE,
    question_id INT NOT NULL REFERENCES questions(question_id) ON DELETE CASCADE,
    order_index INT NOT NULL CHECK (order_index >= 0),
    point_value NUMERIC(6, 2) NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX ux_exam_questions_exam_question ON exam_questions (exam_id, question_id);
CREATE UNIQUE INDEX ux_exam_questions_exam_order ON exam_questions (exam_id, order_index);

CREATE TABLE test_sessions (
    session_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id INT NOT NULL REFERENCES users(user_id),
    exam_id INT NOT NULL REFERENCES exams(exam_id),
    start_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    end_time TIMESTAMP WITH TIME ZONE,
    total_score NUMERIC(7, 2) NOT NULL DEFAULT 0,
    status VARCHAR(32) NOT NULL
);

CREATE INDEX ix_test_sessions_user_id ON test_sessions (user_id);
CREATE INDEX ix_test_sessions_exam_id ON test_sessions (exam_id);

CREATE TABLE responses (
    response_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id UUID NOT NULL REFERENCES test_sessions(session_id) ON DELETE CASCADE,
    question_id INT NOT NULL REFERENCES questions(question_id),
    final_answer TEXT NOT NULL,
    score_earned NUMERIC(7, 2) NOT NULL DEFAULT 0,
    ai_feedback TEXT,
    submitted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_responses_session_id ON responses (session_id);
CREATE INDEX ix_responses_question_id ON responses (question_id);

CREATE TABLE sandbox_logs (
    log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    response_id UUID NOT NULL REFERENCES responses(response_id) ON DELETE CASCADE,
    turn_index INT NOT NULL CHECK (turn_index >= 0),
    user_prompt TEXT NOT NULL,
    ai_response TEXT NOT NULL,
    metrics JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_sandbox_logs_response_id ON sandbox_logs (response_id);
