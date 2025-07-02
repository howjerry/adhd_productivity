# ADHD Productivity System - Security Quick Start Guide

## 🚀 Quick Setup for Developers

### 1. Environment Setup (Required)

```bash
# 1. Copy the environment template
cp .env.example .env

# 2. Generate a secure JWT secret (minimum 32 characters)
# Use a password manager or run this command:
openssl rand -base64 48

# 3. Edit .env file with your values
nano .env
```

### 2. Minimum Required Environment Variables

```bash
# Database (Required)
POSTGRES_DB=adhd_productivity
POSTGRES_USER=your_db_user
POSTGRES_PASSWORD=your_secure_db_password_min_12_chars

# JWT Security (Required)
JWT_SECRET_KEY=your_cryptographically_secure_secret_min_32_chars
JWT_ISSUER=ADHDProductivitySystem
JWT_AUDIENCE=ADHDUsers

# Redis (Optional, leave empty for no password)
REDIS_PASSWORD=
```

### 3. Development Startup

```bash
# Start with Docker Compose
docker-compose up -d

# Or start development profile with pgAdmin
docker-compose --profile development up -d
```

### 4. Security Validation

The application will **refuse to start** if:
- JWT secret is less than 32 characters
- Database password is less than 12 characters
- Any default/example secrets are detected
- Required environment variables are missing

## 🔒 Security Features Active

### Automatic Protection Against:
- ✅ SQL Injection attacks
- ✅ XSS (Cross-site scripting)
- ✅ Path traversal attempts
- ✅ LDAP injection
- ✅ Command injection
- ✅ Oversized requests (>10MB)
- ✅ Invalid content types
- ✅ Suspicious headers

### JWT Security Features:
- ✅ 64-byte cryptographically secure refresh tokens
- ✅ Token versioning (invalidates on user changes)
- ✅ Device tracking capability
- ✅ Comprehensive validation with detailed logging
- ✅ Proper algorithm enforcement (HMAC-SHA256)

### Input Validation:
- ✅ Email format validation
- ✅ Password strength requirements (8+ chars, complexity)
- ✅ Field length limits and character restrictions
- ✅ Query parameter validation
- ✅ Request size limits

## ⚠️ Security Warnings

### ❌ Never Do This:
```bash
# DON'T use weak secrets
JWT_SECRET_KEY=secret123

# DON'T use default passwords
POSTGRES_PASSWORD=password

# DON'T commit .env files
git add .env  # ❌ NEVER!
```

### ✅ Always Do This:
```bash
# DO use strong, random secrets
JWT_SECRET_KEY=7K9X2mN5vQ8wE3rT6yU1pA4sD7fG0hJ3kL6nM9xC2vB5zX8qW1eR4tY7u

# DO use secure passwords (12+ chars, mixed case, numbers, symbols)
POSTGRES_PASSWORD=MyS3cur3D@t@b@se2024!

# DO add .env to .gitignore
echo ".env" >> .gitignore
```

## 🛠️ Development Tips

### Testing Security:
```bash
# Test with invalid JWT (should fail)
curl -H "Authorization: Bearer invalid-token" http://localhost:5000/api/auth/me

# Test injection attempt (should be blocked)
curl "http://localhost:5000/api/tasks?search=' OR 1=1--"

# Check security headers
curl -I http://localhost:5000/api/health
```

### Debugging Security Issues:
```bash
# Check application logs for security events
docker logs adhd-backend | grep -i security

# Validate your JWT secret strength
# Run this in the application logs at startup
```

### Common Issues:

**App won't start?**
- Check `.env` file exists and has all required variables
- Ensure JWT secret is 32+ characters
- Verify database password is 12+ characters
- Check logs: `docker logs adhd-backend`

**Authentication failing?**
- Verify JWT secret matches between services
- Check token expiration (default 60 minutes)
- Review security logs for validation errors

**API requests blocked?**
- Check for injection patterns in request
- Verify content-type headers
- Review request size (<10MB limit)

## 📋 Pre-Production Checklist

### Before Production Deployment:
- [ ] Generate new, unique secrets for production
- [ ] Use proper secret management service
- [ ] Enable HTTPS enforcement
- [ ] Configure production CORS origins
- [ ] Set up security monitoring and alerting
- [ ] Review and update log levels
- [ ] Test all security validations
- [ ] Perform security scan/penetration test

### Environment-Specific Settings:

**Development:**
```bash
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
CORS_ALLOW_CREDENTIALS=true
```

**Production:**
```bash
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
CORS_ALLOW_CREDENTIALS=true
# + Use proper secret management
```

## 🆘 Emergency Security Response

### If Security Breach Suspected:
1. **Immediately rotate all secrets**
2. **Invalidate all active JWT tokens**
3. **Check security logs for attack patterns**
4. **Update to latest security patches**
5. **Notify security team/administrators**

### Quick Secret Rotation:
```bash
# Generate new JWT secret
NEW_SECRET=$(openssl rand -base64 48)
echo "JWT_SECRET_KEY=$NEW_SECRET" >> .env.new

# Generate new database password
NEW_DB_PASS=$(openssl rand -base64 24)
echo "POSTGRES_PASSWORD=$NEW_DB_PASS" >> .env.new

# Replace .env and restart
mv .env.new .env
docker-compose restart
```

---

**Need Help?** Check the full security report: `SECURITY-FIXES-PHASE1.md`