# Development Kickoff Guide

## 🎯 Sprint 1 Overview

Welcome to the ADHD Productivity System development! This guide will get both engineers started with their specific tasks and ensure smooth collaboration.

## 🏃‍♂️ Getting Started

### 1. Environment Setup (Both Engineers)

**First, verify your setup:**
```bash
# Clone and navigate to project
git clone <repository-url>
cd ADHD

# Start the development environment
docker-compose up -d

# Verify services are running
docker-compose ps
```

**Expected output:**
- ✅ PostgreSQL on port 5432
- ✅ Redis on port 6379
- ✅ Backend API on port 3001
- ✅ Frontend on port 3000
- ✅ pgAdmin on port 5050

### 2. Verify Database Setup
```bash
# Check database connection
docker-compose exec postgres psql -U adhd_user -d adhd_productivity -c "\dt"

# Should show all tables: users, tasks, focus_sessions, daily_logs, user_settings
```

## 👨‍💻 Backend Engineer - Start Here

### Week 1 Priority Tasks

#### Day 1-2: Foundation Setup
1. **Navigate to backend directory:**
   ```bash
   cd backend
   npm install
   ```

2. **Create your first endpoint:**
   - Start with `/backend/src/server.js` (already scaffolded)
   - Implement `/backend/src/config/database.js`
   - Test the health endpoint: `curl http://localhost:3001/health`

3. **Database Models:**
   - Create `/backend/src/models/User.js`
   - Create `/backend/src/models/Task.js`
   - Test database connection and basic CRUD operations

#### Day 3-5: Authentication System
1. **Priority order:**
   - User registration endpoint
   - Login endpoint
   - JWT middleware
   - Protected route testing

2. **Key files to create:**
   ```
   /backend/src/controllers/authController.js
   /backend/src/services/authService.js
   /backend/src/middleware/auth.js
   /backend/src/routes/auth.js
   ```

3. **Testing checklist:**
   - [ ] POST /api/v1/auth/register works
   - [ ] POST /api/v1/auth/login returns JWT
   - [ ] Protected routes require valid token
   - [ ] Password hashing works correctly

### Week 2 Focus
- Task CRUD operations
- User profile management
- Basic analytics endpoints

### Daily Sync Points
- 9:00 AM: Quick standup with frontend engineer
- 5:00 PM: Integration testing with frontend

---

## 👩‍💻 Frontend Engineer - Start Here

### Week 1 Priority Tasks

#### Day 1-2: React Foundation
1. **Navigate to frontend directory:**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

2. **Verify setup:**
   - Visit http://localhost:3000
   - Should see placeholder login page
   - Tailwind CSS should be working

3. **Create basic structure:**
   ```
   /frontend/src/components/common/Button.jsx
   /frontend/src/components/common/Input.jsx
   /frontend/src/components/common/LoadingSpinner.jsx
   ```

#### Day 3-5: Authentication UI
1. **Priority order:**
   - Login form component
   - Registration form component
   - Authentication state management
   - Protected route wrapper

2. **Key files to create:**
   ```
   /frontend/src/components/auth/LoginForm.jsx
   /frontend/src/components/auth/RegisterForm.jsx
   /frontend/src/store/authStore.js
   /frontend/src/services/authService.js
   ```

3. **Testing checklist:**
   - [ ] Login form validates input
   - [ ] Registration form works
   - [ ] Auth state persists on refresh
   - [ ] Protected routes redirect correctly

### Week 2 Focus
- Dashboard interface
- Task management UI
- Layout and navigation components

### Integration with Backend
- Start with mock data while backend is being built
- Switch to real API calls once auth endpoints are ready
- Daily coordination on API contract changes

---

## 🔄 Daily Workflow

### Morning Standup (9:00 AM)
**Each engineer answers:**
1. What did I complete yesterday?
2. What will I work on today?
3. Any blockers or dependencies?

### Integration Points
**Critical handoff moments:**
1. **Day 3:** Backend auth endpoints → Frontend auth integration
2. **Day 7:** Task API endpoints → Frontend task components
3. **Day 10:** Complete integration testing

### End of Day Sync (5:00 PM)
**Quick check:**
- Demo progress to each other
- Identify next day's integration needs
- Update task status

---

## 📋 Task Tracking

### Backend Engineer - Week 1 Checklist
- [ ] **BE-1.1** Development environment setup (Day 1)
- [ ] **BE-1.2** Database models and migrations (Day 2)
- [ ] **BE-1.3** Authentication system (Days 3-5)
- [ ] **BE-1.4** Basic API structure (Days 4-5)

### Frontend Engineer - Week 1 Checklist
- [ ] **FE-1.1** Project setup and configuration (Day 1)
- [ ] **FE-1.2** Authentication UI components (Days 2-4)
- [ ] **FE-1.3** Layout and navigation (Days 4-5)
- [ ] **FE-1.4** UI component library (Days 2-5)

---

## 🛠️ Development Tools

### Backend Tools
- **Testing:** `npm test` (Jest)
- **Linting:** `npm run lint`
- **Database:** pgAdmin at http://localhost:5050
- **API Testing:** Postman/Insomnia recommended

### Frontend Tools
- **Dev Server:** `npm run dev`
- **Testing:** `npm test` (Vitest)
- **Linting:** `npm run lint`
- **Build:** `npm run build`

### Shared Tools
- **Docker:** `docker-compose logs <service>` for debugging
- **Git:** Feature branch workflow
- **Communication:** Daily standups + Slack/email for async

---

## 🚨 Common Issues & Solutions

### Backend Issues
1. **Database connection fails:**
   ```bash
   docker-compose restart postgres
   docker-compose logs postgres
   ```

2. **Port already in use:**
   ```bash
   lsof -i :3001
   kill -9 <PID>
   ```

3. **JWT token issues:**
   - Check JWT_SECRET in .env
   - Verify token format in Authorization header

### Frontend Issues
1. **Tailwind styles not working:**
   ```bash
   npm run build
   # Check tailwind.config.js paths
   ```

2. **API calls failing:**
   - Check vite.config.js proxy settings
   - Verify backend is running
   - Check CORS configuration

3. **Hot reload not working:**
   ```bash
   rm -rf node_modules
   npm install
   npm run dev
   ```

---

## 📞 Support & Communication

### Primary Communication
- **Daily Standups:** Google Meet/Zoom
- **Async Updates:** Slack/Discord
- **Code Review:** GitHub Pull Requests
- **Documentation:** Update README.md

### Escalation Path
1. Try to resolve together (15 minutes)
2. Check documentation and existing code
3. Search for similar issues online
4. Create detailed issue in GitHub

### Success Metrics
- [ ] All Sprint 1 MVP features complete
- [ ] Frontend and backend fully integrated
- [ ] All tests passing
- [ ] Application deployable via Docker
- [ ] No critical bugs in core functionality

---

## 🎉 Sprint 1 Success Criteria

### Week 1 Goals
- ✅ Development environment fully operational
- ✅ Authentication system working end-to-end
- ✅ Basic UI components created
- ✅ Database integration complete

### Week 2 Goals
- ✅ Task management functionality
- ✅ Dashboard interface complete
- ✅ User profile management
- ✅ Full application integration

### Final Deliverable
A working ADHD Productivity System with:
- User registration and login
- Task creation and management
- Basic dashboard interface
- Responsive design
- Docker deployment ready

**Let's build something amazing! 🚀**