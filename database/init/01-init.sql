-- ADHD 生產力系統 - 資料庫初始化腳本
-- 此腳本將在 PostgreSQL 容器啟動時自動執行

-- 建立必要的擴充功能
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- 建立資料庫 (如果不存在)
SELECT 'CREATE DATABASE adhd_productivity'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'adhd_productivity')\gexec

-- 切換到應用資料庫
\c adhd_productivity;

-- 建立 ADHD 類型枚舉
CREATE TYPE adhd_type AS ENUM ('Inattentive', 'Hyperactive', 'Combined');

-- 建立任務狀態枚舉
CREATE TYPE task_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Archived');

-- 建立任務優先級枚舉
CREATE TYPE task_priority AS ENUM ('Low', 'Medium', 'High', 'Urgent');

-- 建立能量等級枚舉
CREATE TYPE energy_level AS ENUM ('High', 'Medium', 'Low', 'Depleted');

-- 建立使用者表
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    adhd_type adhd_type,
    timezone VARCHAR(50) DEFAULT 'UTC',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT true
);

-- 建立任務表
CREATE TABLE IF NOT EXISTS tasks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    parent_task_id UUID REFERENCES tasks(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    status task_status DEFAULT 'NotStarted',
    priority task_priority DEFAULT 'Medium',
    estimated_minutes INTEGER,
    actual_minutes INTEGER,
    required_energy energy_level DEFAULT 'Medium',
    due_date TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    tags TEXT[],
    is_archived BOOLEAN DEFAULT false
);

-- 建立捕捉項目表
CREATE TABLE IF NOT EXISTS capture_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    source VARCHAR(50) DEFAULT 'manual',
    is_processed BOOLEAN DEFAULT false,
    processed_at TIMESTAMP WITH TIME ZONE,
    converted_to_task_id UUID REFERENCES tasks(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 建立時間區塊表
CREATE TABLE IF NOT EXISTS time_blocks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    title VARCHAR(255) NOT NULL,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    block_type VARCHAR(50) DEFAULT 'work',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 建立計時器會話表
CREATE TABLE IF NOT EXISTS timer_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    session_type VARCHAR(50) DEFAULT 'pomodoro',
    planned_duration INTEGER NOT NULL,
    actual_duration INTEGER,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL,
    ended_at TIMESTAMP WITH TIME ZONE,
    is_completed BOOLEAN DEFAULT false,
    break_duration INTEGER DEFAULT 5,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 建立使用者進度表
CREATE TABLE IF NOT EXISTS user_progress (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    tasks_completed INTEGER DEFAULT 0,
    total_focus_minutes INTEGER DEFAULT 0,
    pomodoro_sessions INTEGER DEFAULT 0,
    energy_level energy_level,
    mood_rating INTEGER CHECK (mood_rating >= 1 AND mood_rating <= 10),
    productivity_rating INTEGER CHECK (productivity_rating >= 1 AND productivity_rating <= 10),
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, date)
);

-- 建立索引以提升查詢效能
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_tasks_user_id ON tasks(user_id);
CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks(status);
CREATE INDEX IF NOT EXISTS idx_tasks_priority ON tasks(priority);
CREATE INDEX IF NOT EXISTS idx_tasks_due_date ON tasks(due_date);
CREATE INDEX IF NOT EXISTS idx_tasks_parent_id ON tasks(parent_task_id);
CREATE INDEX IF NOT EXISTS idx_capture_items_user_id ON capture_items(user_id);
CREATE INDEX IF NOT EXISTS idx_capture_items_processed ON capture_items(is_processed);
CREATE INDEX IF NOT EXISTS idx_time_blocks_user_id ON time_blocks(user_id);
CREATE INDEX IF NOT EXISTS idx_time_blocks_task_id ON time_blocks(task_id);
CREATE INDEX IF NOT EXISTS idx_time_blocks_time_range ON time_blocks(start_time, end_time);
CREATE INDEX IF NOT EXISTS idx_timer_sessions_user_id ON timer_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_timer_sessions_task_id ON timer_sessions(task_id);
CREATE INDEX IF NOT EXISTS idx_user_progress_user_date ON user_progress(user_id, date);

-- 建立更新 updated_at 的觸發器函數
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 為需要的表建立觸發器
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tasks_updated_at BEFORE UPDATE ON tasks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_time_blocks_updated_at BEFORE UPDATE ON time_blocks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_user_progress_updated_at BEFORE UPDATE ON user_progress
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- 建立示例使用者 (測試用)
INSERT INTO users (username, email, password_hash, first_name, last_name, adhd_type) 
VALUES 
    ('demo_user', 'demo@adhd.dev', crypt('demo123', gen_salt('bf')), 'Demo', 'User', 'Combined'),
    ('test_user', 'test@adhd.dev', crypt('test123', gen_salt('bf')), 'Test', 'User', 'Inattentive')
ON CONFLICT (email) DO NOTHING;

-- 為示例使用者建立一些測試資料
DO $$
DECLARE
    demo_user_id UUID;
    test_user_id UUID;
    task1_id UUID;
    task2_id UUID;
BEGIN
    -- 取得使用者 ID
    SELECT id INTO demo_user_id FROM users WHERE email = 'demo@adhd.dev';
    SELECT id INTO test_user_id FROM users WHERE email = 'test@adhd.dev';
    
    -- 為 demo 使用者建立示例任務
    INSERT INTO tasks (user_id, title, description, priority, status, estimated_minutes, required_energy)
    VALUES 
        (demo_user_id, '完成專案報告', '撰寫Q4專案進度報告，包含成果分析和下階段規劃', 'High', 'InProgress', 120, 'High'),
        (demo_user_id, '回覆重要郵件', '處理客戶詢問和內部溝通郵件', 'Medium', 'NotStarted', 30, 'Medium'),
        (demo_user_id, '整理桌面', '清理工作區域，整理文件和用品', 'Low', 'NotStarted', 15, 'Low'),
        (demo_user_id, '閱讀技術文章', '學習新的開發技術和最佳實踐', 'Medium', 'NotStarted', 45, 'Medium')
    RETURNING id INTO task1_id;
    
    -- 建立子任務
    INSERT INTO tasks (user_id, parent_task_id, title, description, priority, status, estimated_minutes)
    VALUES 
        (demo_user_id, task1_id, '收集專案數據', '整理Q4的關鍵績效指標', 'High', 'Completed', 30),
        (demo_user_id, task1_id, '撰寫執行摘要', '總結專案重點和成果', 'High', 'InProgress', 45);
    
    -- 建立捕捉項目
    INSERT INTO capture_items (user_id, content, source)
    VALUES 
        (demo_user_id, '記得買咖啡豆', 'manual'),
        (demo_user_id, '研究新的時間管理技巧', 'manual'),
        (demo_user_id, '預約醫生檢查', 'manual');
    
    -- 建立時間區塊
    INSERT INTO time_blocks (user_id, task_id, title, start_time, end_time, block_type)
    VALUES 
        (demo_user_id, task1_id, '專案報告工作時間', 
         CURRENT_TIMESTAMP + INTERVAL '1 hour', 
         CURRENT_TIMESTAMP + INTERVAL '3 hours', 'work'),
        (demo_user_id, NULL, '午休時間', 
         CURRENT_TIMESTAMP + INTERVAL '6 hours', 
         CURRENT_TIMESTAMP + INTERVAL '7 hours', 'break');
    
    -- 建立進度記錄
    INSERT INTO user_progress (user_id, date, tasks_completed, total_focus_minutes, pomodoro_sessions, energy_level, mood_rating, productivity_rating)
    VALUES 
        (demo_user_id, CURRENT_DATE, 2, 90, 4, 'Medium', 7, 8),
        (demo_user_id, CURRENT_DATE - 1, 3, 120, 5, 'High', 8, 9);
        
END $$;

-- 顯示初始化完成訊息
SELECT 'ADHD 生產力系統資料庫初始化完成！' AS message;