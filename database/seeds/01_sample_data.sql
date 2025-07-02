-- Sample data for development and testing
-- This file will be executed after schema creation

-- Insert sample user
INSERT INTO users (id, email, password_hash, first_name, last_name, timezone, preferences)
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', 'demo@adhd.com', '$2b$10$rOxR9v7/tpxdGfmZcH6oMuF2wB8XMWzSNXcZVxrCbqzqBpKDXYbEu', 'Demo', 'User', 'America/New_York', '{"theme": "light", "notifications": true}');

-- Insert sample tasks
INSERT INTO tasks (user_id, title, description, priority, status, estimated_duration, due_date)
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', 'Complete project proposal', 'Write and submit the Q1 project proposal', 'high', 'in_progress', 120, CURRENT_TIMESTAMP + INTERVAL '2 days'),
    ('550e8400-e29b-41d4-a716-446655440000', 'Review team feedback', 'Go through all team feedback from last sprint', 'medium', 'pending', 60, CURRENT_TIMESTAMP + INTERVAL '1 day'),
    ('550e8400-e29b-41d4-a716-446655440000', 'Update documentation', 'Update API documentation with new endpoints', 'low', 'pending', 90, CURRENT_TIMESTAMP + INTERVAL '3 days'),
    ('550e8400-e29b-41d4-a716-446655440000', 'Bug fixes', 'Fix critical bugs reported in production', 'urgent', 'completed', 180, CURRENT_TIMESTAMP - INTERVAL '1 day');

-- Insert sample focus sessions
INSERT INTO focus_sessions (user_id, task_id, duration, break_duration, started_at, completed_at, interrupted)
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', (SELECT id FROM tasks WHERE title = 'Complete project proposal'), 25, 5, CURRENT_TIMESTAMP - INTERVAL '2 hours', CURRENT_TIMESTAMP - INTERVAL '1 hour 30 minutes', false),
    ('550e8400-e29b-41d4-a716-446655440000', (SELECT id FROM tasks WHERE title = 'Bug fixes'), 25, 5, CURRENT_TIMESTAMP - INTERVAL '4 hours', CURRENT_TIMESTAMP - INTERVAL '3 hours 30 minutes', false),
    ('550e8400-e29b-41d4-a716-446655440000', (SELECT id FROM tasks WHERE title = 'Complete project proposal'), 15, 0, CURRENT_TIMESTAMP - INTERVAL '1 hour', CURRENT_TIMESTAMP - INTERVAL '45 minutes', true);

-- Insert sample daily log
INSERT INTO daily_logs (user_id, date, mood, energy_level, focus_score, tasks_completed, total_focus_time, notes)
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', CURRENT_DATE, 4, 3, 7, 1, 65, 'Good focus session in the morning, energy dropped after lunch');

-- Insert sample user settings
INSERT INTO user_settings (user_id, setting_key, setting_value)
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', 'pomodoro_duration', '25'),
    ('550e8400-e29b-41d4-a716-446655440000', 'short_break_duration', '5'),
    ('550e8400-e29b-41d4-a716-446655440000', 'long_break_duration', '15'),
    ('550e8400-e29b-41d4-a716-446655440000', 'notification_preferences', '{"email": true, "push": false, "sound": true}');