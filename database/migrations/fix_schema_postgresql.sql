-- ADHD Productivity System - PostgreSQL Schema Fix Migration
-- This script addresses the critical mismatches between the C# Domain entities and PostgreSQL schema

-- Drop existing tables if they exist (BE CAREFUL - this will delete data)
-- Uncomment the following lines only if you want to recreate the schema from scratch
-- DROP TABLE IF EXISTS timer_sessions CASCADE;
-- DROP TABLE IF EXISTS user_progress CASCADE;
-- DROP TABLE IF EXISTS time_blocks CASCADE;
-- DROP TABLE IF EXISTS capture_items CASCADE;
-- DROP TABLE IF EXISTS tasks CASCADE;
-- DROP TABLE IF EXISTS users CASCADE;

-- Create updated Users table to match C# Domain entity
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(256) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,  -- Changed from first_name/last_name to single name
    password_hash VARCHAR(500) NOT NULL,
    password_salt VARCHAR(500) NOT NULL,  -- Added missing field
    adhd_type INTEGER NOT NULL DEFAULT 0,  -- Added enum field (Combined = 0)
    time_zone VARCHAR(50) DEFAULT 'UTC',
    preferred_theme INTEGER NOT NULL DEFAULT 0,  -- Added enum field (Light = 0)
    is_onboarding_completed BOOLEAN DEFAULT FALSE,  -- Added missing field
    last_active_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,  -- Added missing field
    profile_picture_url VARCHAR(500),  -- Added missing field
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(256),  -- Added audit field
    updated_by VARCHAR(256)   -- Added audit field
);

-- Create updated Tasks table to match C# Domain entity
CREATE TABLE IF NOT EXISTS tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,  -- Reduced from 255 to match C# constraint
    description TEXT,  -- Changed from VARCHAR to TEXT for 2000 char limit
    status INTEGER NOT NULL DEFAULT 0,  -- Changed to INTEGER enum (Todo = 0)
    priority INTEGER NOT NULL DEFAULT 1,  -- Changed to INTEGER enum (Medium = 1)
    estimated_minutes INTEGER,  -- Renamed from estimated_duration
    actual_minutes INTEGER DEFAULT 0,  -- Renamed from actual_duration
    due_date TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    tags VARCHAR(500) DEFAULT '',  -- Added missing field
    notes TEXT,  -- Added missing field (2000 char limit)
    is_recurring BOOLEAN DEFAULT FALSE,  -- Added missing field
    recurrence_pattern VARCHAR(100),  -- Added missing field
    next_occurrence TIMESTAMP WITH TIME ZONE,  -- Added missing field
    parent_task_id UUID REFERENCES tasks(id) ON DELETE RESTRICT,  -- Added missing field for subtasks
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(256),  -- Added audit field
    updated_by VARCHAR(256)   -- Added audit field
);

-- Create Capture Items table (missing from PostgreSQL schema)
CREATE TABLE IF NOT EXISTS capture_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content VARCHAR(2000) NOT NULL,
    type INTEGER NOT NULL DEFAULT 0,  -- CaptureType enum
    priority INTEGER NOT NULL DEFAULT 1,  -- Priority enum
    is_processed BOOLEAN DEFAULT FALSE,
    processed_at TIMESTAMP WITH TIME ZONE,
    tags VARCHAR(500) DEFAULT '',
    context VARCHAR(200),
    task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    energy_level INTEGER DEFAULT 1,
    mood VARCHAR(50),
    is_urgent BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(256),
    updated_by VARCHAR(256)
);

-- Create Time Blocks table (missing from PostgreSQL schema)
CREATE TABLE IF NOT EXISTS time_blocks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    type INTEGER NOT NULL DEFAULT 0,  -- TimeBlockType enum
    color VARCHAR(7) DEFAULT '#007BFF',
    is_recurring BOOLEAN DEFAULT FALSE,
    recurrence_pattern VARCHAR(100),
    is_flexible BOOLEAN DEFAULT FALSE,
    task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    is_completed BOOLEAN DEFAULT FALSE,
    completion_notes VARCHAR(1000),
    actual_start_time TIMESTAMP WITH TIME ZONE,
    actual_end_time TIMESTAMP WITH TIME ZONE,
    energy_level INTEGER,
    focus_level INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(256),
    updated_by VARCHAR(256)
);

-- Create Timer Sessions table (missing from PostgreSQL schema)
CREATE TABLE IF NOT EXISTS timer_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    type INTEGER NOT NULL DEFAULT 0,  -- TimerType enum
    planned_minutes INTEGER NOT NULL,
    actual_minutes INTEGER DEFAULT 0,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE,
    status INTEGER NOT NULL DEFAULT 0,  -- TimerStatus enum
    is_completed BOOLEAN DEFAULT FALSE,
    notes VARCHAR(1000),
    interruptions INTEGER DEFAULT 0,
    focus_level INTEGER,
    start_energy_level INTEGER,
    end_energy_level INTEGER,
    tags VARCHAR(500) DEFAULT '',
    accomplishments VARCHAR(1000),
    challenges VARCHAR(1000),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(256),
    updated_by VARCHAR(256)
);

-- Create User Progress table (missing from PostgreSQL schema)
CREATE TABLE IF NOT EXISTS user_progress (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    tasks_completed INTEGER DEFAULT 0,
    minutes_worked INTEGER DEFAULT 0,
    pomodoro_sessions INTEGER DEFAULT 0,
    capture_items_processed INTEGER DEFAULT 0,
    mood INTEGER DEFAULT 3,  -- Scale 1-5
    energy_level INTEGER DEFAULT 3,  -- Scale 1-5
    focus_level INTEGER DEFAULT 5,  -- Scale 1-10
    stress_level INTEGER DEFAULT 3,  -- Scale 1-5
    sleep_quality INTEGER DEFAULT 3,  -- Scale 1-5
    hours_slept DECIMAL(3,1) DEFAULT 8.0,
    notes VARCHAR(1000),
    went_well VARCHAR(1000),
    to_improve VARCHAR(1000),
    tomorrow_goals VARCHAR(1000),
    medication_taken BOOLEAN DEFAULT FALSE,
    exercise_minutes INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(256),
    updated_by VARCHAR(256),
    UNIQUE(user_id, date)
);

-- Create optimized indexes for performance
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_last_active_at ON users(last_active_at);

CREATE INDEX IF NOT EXISTS idx_tasks_user_id ON tasks(user_id);
CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks(status);
CREATE INDEX IF NOT EXISTS idx_tasks_priority ON tasks(priority);
CREATE INDEX IF NOT EXISTS idx_tasks_due_date ON tasks(due_date);
CREATE INDEX IF NOT EXISTS idx_tasks_parent_task_id ON tasks(parent_task_id);
CREATE INDEX IF NOT EXISTS idx_tasks_user_status ON tasks(user_id, status);  -- Composite index for common query pattern

CREATE INDEX IF NOT EXISTS idx_capture_items_user_id ON capture_items(user_id);
CREATE INDEX IF NOT EXISTS idx_capture_items_type ON capture_items(type);
CREATE INDEX IF NOT EXISTS idx_capture_items_is_processed ON capture_items(is_processed);
CREATE INDEX IF NOT EXISTS idx_capture_items_created_at ON capture_items(created_at);
CREATE INDEX IF NOT EXISTS idx_capture_items_task_id ON capture_items(task_id);

CREATE INDEX IF NOT EXISTS idx_time_blocks_user_id ON time_blocks(user_id);
CREATE INDEX IF NOT EXISTS idx_time_blocks_start_time ON time_blocks(start_time);
CREATE INDEX IF NOT EXISTS idx_time_blocks_type ON time_blocks(type);
CREATE INDEX IF NOT EXISTS idx_time_blocks_task_id ON time_blocks(task_id);

CREATE INDEX IF NOT EXISTS idx_timer_sessions_user_id ON timer_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_timer_sessions_start_time ON timer_sessions(start_time);
CREATE INDEX IF NOT EXISTS idx_timer_sessions_type ON timer_sessions(type);
CREATE INDEX IF NOT EXISTS idx_timer_sessions_status ON timer_sessions(status);
CREATE INDEX IF NOT EXISTS idx_timer_sessions_task_id ON timer_sessions(task_id);

CREATE INDEX IF NOT EXISTS idx_user_progress_user_id ON user_progress(user_id);
CREATE INDEX IF NOT EXISTS idx_user_progress_date ON user_progress(date);
CREATE INDEX IF NOT EXISTS idx_user_progress_user_date ON user_progress(user_id, date);

-- Create function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tasks_updated_at BEFORE UPDATE ON tasks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_capture_items_updated_at BEFORE UPDATE ON capture_items
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_time_blocks_updated_at BEFORE UPDATE ON time_blocks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_timer_sessions_updated_at BEFORE UPDATE ON timer_sessions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_user_progress_updated_at BEFORE UPDATE ON user_progress
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Insert sample data for testing (optional)
INSERT INTO users (email, name, password_hash, password_salt, adhd_type) 
VALUES 
    ('demo@adhd.dev', 'Demo User', 'demo_hash', 'demo_salt', 0),
    ('test@adhd.dev', 'Test User', 'test_hash', 'test_salt', 1),
    ('admin@adhd.dev', 'Admin User', 'admin_hash', 'admin_salt', 2)
ON CONFLICT (email) DO NOTHING;