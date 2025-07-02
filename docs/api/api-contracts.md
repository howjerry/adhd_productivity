# API Contracts and Integration Points

## Base URL
- Development: `http://localhost:3001/api/v1`
- Production: `https://api.adhd-productivity.com/api/v1`

## Authentication
All protected endpoints require a JWT token in the Authorization header:
```
Authorization: Bearer <jwt_token>
```

## Response Format
All API responses follow this structure:
```json
{
  "success": boolean,
  "data": object | array | null,
  "message": string,
  "error": string | null,
  "timestamp": "ISO 8601 string"
}
```

## Core API Endpoints

### Authentication
```
POST /auth/register
POST /auth/login
POST /auth/logout
POST /auth/refresh
GET  /auth/me
```

### Users
```
GET    /users/profile
PUT    /users/profile
GET    /users/settings
PUT    /users/settings
DELETE /users/account
```

### Tasks
```
GET    /tasks              # Get user's tasks with filters
POST   /tasks              # Create new task
GET    /tasks/:id          # Get specific task
PUT    /tasks/:id          # Update task
DELETE /tasks/:id          # Delete task
PATCH  /tasks/:id/status   # Update task status
```

### Focus Sessions
```
GET    /focus-sessions               # Get user's focus sessions
POST   /focus-sessions               # Start new focus session
GET    /focus-sessions/:id          # Get specific session
PUT    /focus-sessions/:id          # Update session
DELETE /focus-sessions/:id          # Delete session
PATCH  /focus-sessions/:id/complete # Complete session
```

### Analytics
```
GET /analytics/dashboard    # Dashboard statistics
GET /analytics/productivity # Productivity metrics
GET /analytics/focus        # Focus session analytics
GET /analytics/trends       # Trend analysis
```

### Daily Logs
```
GET    /daily-logs         # Get user's daily logs
POST   /daily-logs         # Create daily log entry
GET    /daily-logs/:date   # Get specific date log
PUT    /daily-logs/:date   # Update daily log
DELETE /daily-logs/:date   # Delete daily log
```

## Request/Response Examples

### User Registration
**Request:**
```json
POST /auth/register
{
  "email": "user@example.com",
  "password": "securePassword123",
  "firstName": "John",
  "lastName": "Doe",
  "timezone": "America/New_York"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "user": {
      "id": "uuid",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "timezone": "America/New_York"
    },
    "token": "jwt_token",
    "refreshToken": "refresh_token"
  },
  "message": "User registered successfully",
  "error": null,
  "timestamp": "2024-01-01T00:00:00.000Z"
}
```

### Create Task
**Request:**
```json
POST /tasks
{
  "title": "Complete project proposal",
  "description": "Write and submit the Q1 project proposal",
  "priority": "high",
  "estimatedDuration": 120,
  "dueDate": "2024-01-15T10:00:00.000Z"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "task_uuid",
    "title": "Complete project proposal",
    "description": "Write and submit the Q1 project proposal",
    "priority": "high",
    "status": "pending",
    "estimatedDuration": 120,
    "actualDuration": null,
    "dueDate": "2024-01-15T10:00:00.000Z",
    "createdAt": "2024-01-01T00:00:00.000Z",
    "updatedAt": "2024-01-01T00:00:00.000Z"
  },
  "message": "Task created successfully",
  "error": null,
  "timestamp": "2024-01-01T00:00:00.000Z"
}
```

### Start Focus Session
**Request:**
```json
POST /focus-sessions
{
  "taskId": "task_uuid",
  "duration": 25,
  "breakDuration": 5
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "session_uuid",
    "taskId": "task_uuid",
    "duration": 25,
    "breakDuration": 5,
    "startedAt": "2024-01-01T10:00:00.000Z",
    "completedAt": null,
    "interrupted": false,
    "status": "active"
  },
  "message": "Focus session started successfully",
  "error": null,
  "timestamp": "2024-01-01T10:00:00.000Z"
}
```

## Error Handling

### HTTP Status Codes
- 200: Success
- 201: Created
- 400: Bad Request
- 401: Unauthorized
- 403: Forbidden
- 404: Not Found
- 409: Conflict
- 422: Validation Error
- 500: Internal Server Error

### Error Response Format
```json
{
  "success": false,
  "data": null,
  "message": "Error description",
  "error": "Detailed error information",
  "timestamp": "2024-01-01T00:00:00.000Z",
  "validationErrors": [
    {
      "field": "email",
      "message": "Invalid email format"
    }
  ]
}
```

## Rate Limiting
- Standard endpoints: 100 requests per minute per user
- Authentication endpoints: 5 requests per minute per IP
- File upload endpoints: 10 requests per minute per user

## WebSocket Events (Real-time Updates)

### Connection
```javascript
const socket = io('ws://localhost:3001', {
  auth: {
    token: 'jwt_token'
  }
});
```

### Events
```javascript
// Focus session updates
socket.on('focus-session:started', (data) => {});
socket.on('focus-session:completed', (data) => {});
socket.on('focus-session:interrupted', (data) => {});

// Task updates
socket.on('task:created', (data) => {});
socket.on('task:updated', (data) => {});
socket.on('task:completed', (data) => {});

// Notifications
socket.on('notification:new', (data) => {});
```